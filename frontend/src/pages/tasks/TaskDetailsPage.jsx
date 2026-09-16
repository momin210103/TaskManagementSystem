import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import {
  ArrowLeft,
  Calendar,
  User,
  Users2,
  Clock,
  MessageSquare,
  Send,
  Trash2,
  Edit2,
  AlertTriangle,
  CheckCircle2
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { taskService } from '../../services/taskService';
import { commentService } from '../../services/commentService';
import { teamService } from '../../services/teamService';
import { StatusBadge } from '../../components/common/StatusBadge';
import { PriorityBadge } from '../../components/common/PriorityBadge';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { EmptyState } from '../../components/common/EmptyState';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { Modal } from '../../components/common/Modal';
import { TaskFormModal } from './TaskFormModal';
import { formatDate, formatDateTime, getInitials } from '../../utils/formatters';
import { STATUS_OPTIONS } from '../../utils/constants';

export function TaskDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user, isAdmin, isManager } = useAuth();

  const [task, setTask] = useState(null);
  const [comments, setComments] = useState([]);
  const [newComment, setNewComment] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmittingComment, setIsSubmittingComment] = useState(false);
  const [error, setError] = useState(null);

  // Modals
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [teamMembers, setTeamMembers] = useState([]);
  const [selectedAssignee, setSelectedAssignee] = useState('');
  const [isAssigning, setIsAssigning] = useState(false);

  // Delete Modals
  const [isDeleteTaskOpen, setIsDeleteTaskOpen] = useState(false);
  const [isDeletingTask, setIsDeletingTask] = useState(false);
  const [deletingCommentId, setDeletingCommentId] = useState(null);
  const [isDeletingComment, setIsDeletingComment] = useState(false);

  const canManage = isAdmin || isManager;

  const loadTaskData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [taskData, commentsData] = await Promise.all([
        taskService.getTaskById(id),
        commentService.getComments(id)
      ]);
      setTask(taskData);
      setComments(commentsData);
    } catch (err) {
      setError(err.message || 'Failed to load task details.');
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadTaskData();
  }, [loadTaskData]);

  const handleStatusChange = async (newStatus) => {
    try {
      const updated = await taskService.updateTaskStatus(id, newStatus);
      setTask(updated);
    } catch (err) {
      setError(err.message || 'Failed to update task status.');
    }
  };

  const handleOpenAssignModal = async () => {
    if (!task?.teamId) return;
    try {
      const teamDetails = await teamService.getTeamById(task.teamId);
      setTeamMembers(teamDetails.members || []);
      setSelectedAssignee(task.assignedToId || '');
      setIsAssignModalOpen(true);
    } catch (err) {
      setError('Failed to load team members: ' + err.message);
    }
  };

  const handleAssignSubmit = async (e) => {
    e.preventDefault();
    if (!selectedAssignee) return;
    setIsAssigning(true);
    try {
      const updated = await taskService.assignTask(id, selectedAssignee);
      setTask(updated);
      setIsAssignModalOpen(false);
    } catch (err) {
      setError(err.message || 'Failed to assign task.');
    } finally {
      setIsAssigning(false);
    }
  };

  const handleAddComment = async (e) => {
    e.preventDefault();
    if (!newComment.trim()) return;

    setIsSubmittingComment(true);
    try {
      const created = await commentService.createComment(id, newComment.trim());
      setComments((prev) => [...prev, created]);
      setNewComment('');
    } catch (err) {
      setError(err.message || 'Failed to post comment.');
    } finally {
      setIsSubmittingComment(false);
    }
  };

  const handleDeleteComment = async () => {
    if (!deletingCommentId) return;
    setIsDeletingComment(true);
    try {
      await commentService.deleteComment(id, deletingCommentId);
      setComments((prev) => prev.filter((c) => c.id !== deletingCommentId));
      setDeletingCommentId(null);
    } catch (err) {
      setError(err.message || 'Failed to delete comment.');
    } finally {
      setIsDeletingComment(false);
    }
  };

  const handleDeleteTask = async () => {
    setIsDeletingTask(true);
    try {
      await taskService.deleteTask(id);
      navigate('/tasks', { replace: true });
    } catch (err) {
      setError(err.message || 'Failed to delete task.');
      setIsDeletingTask(false);
    }
  };

  if (isLoading) {
    return <LoadingSpinner fullScreen text="Loading task details..." />;
  }

  if (!task && !isLoading) {
    return (
      <div className="p-8">
        <ErrorMessage message={error || 'Task not found.'} />
        <Link to="/tasks" className="inline-flex items-center gap-2 text-indigo-600 font-semibold hover:underline mt-4">
          <ArrowLeft className="w-4 h-4" /> Back to Tasks
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-8 max-w-5xl mx-auto animate-fade-in">
      {/* Top Breadcrumb & Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <Link
          to="/tasks"
          className="inline-flex items-center gap-2 text-sm font-medium text-slate-500 hover:text-slate-800 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" /> Back to Tasks
        </Link>

        {canManage && (
          <div className="flex items-center gap-2">
            <button
              onClick={() => setIsEditModalOpen(true)}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors shadow-sm"
            >
              <Edit2 className="w-3.5 h-3.5" /> Edit
            </button>
            <button
              onClick={handleOpenAssignModal}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-indigo-700 bg-indigo-50 border border-indigo-200 rounded-lg hover:bg-indigo-100 transition-colors"
            >
              <User className="w-3.5 h-3.5" /> Reassign
            </button>
            <button
              onClick={() => setIsDeleteTaskOpen(true)}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-rose-700 bg-rose-50 border border-rose-200 rounded-lg hover:bg-rose-100 transition-colors"
            >
              <Trash2 className="w-3.5 h-3.5" /> Delete
            </button>
          </div>
        )}
      </div>

      <ErrorMessage message={error} onDismiss={() => setError(null)} />

      {/* Task Overview Card */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-sm p-6 sm:p-8 space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2.5 mb-2">
              <StatusBadge status={task.status} />
              <PriorityBadge priority={task.priority} />
            </div>
            <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">{task.title}</h1>
          </div>

          {/* Quick Status Updater */}
          <div className="flex items-center gap-2 bg-slate-50 border border-slate-200 px-3 py-2 rounded-xl">
            <span className="text-xs font-semibold text-slate-500 uppercase">Status:</span>
            <select
              value={task.status}
              onChange={(e) => handleStatusChange(e.target.value)}
              className="text-xs font-semibold bg-white border border-slate-200 rounded-lg px-2 py-1 text-slate-800 focus:ring-1 focus:ring-indigo-500"
            >
              {STATUS_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Description */}
        <div className="text-sm text-slate-700 leading-relaxed bg-slate-50/50 p-4 rounded-xl border border-slate-100">
          {task.description || <span className="italic text-slate-400">No description provided for this task.</span>}
        </div>

        {/* Metadata Details Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 pt-4 border-t border-slate-100 text-xs">
          <div className="p-3 bg-slate-50 rounded-xl">
            <div className="flex items-center gap-1.5 text-slate-500 font-medium mb-1">
              <Calendar className="w-3.5 h-3.5" /> Deadline
            </div>
            <div className="font-semibold text-slate-900 text-sm">{formatDate(task.deadline)}</div>
          </div>

          <div className="p-3 bg-slate-50 rounded-xl">
            <div className="flex items-center gap-1.5 text-slate-500 font-medium mb-1">
              <Users2 className="w-3.5 h-3.5" /> Team
            </div>
            <div className="font-semibold text-slate-900 text-sm">{task.teamName || '—'}</div>
          </div>

          <div className="p-3 bg-slate-50 rounded-xl">
            <div className="flex items-center gap-1.5 text-slate-500 font-medium mb-1">
              <User className="w-3.5 h-3.5" /> Assigned To
            </div>
            <div className="font-semibold text-slate-900 text-sm">{task.assignedToName || 'Unassigned'}</div>
          </div>

          <div className="p-3 bg-slate-50 rounded-xl">
            <div className="flex items-center gap-1.5 text-slate-500 font-medium mb-1">
              <User className="w-3.5 h-3.5" /> Assigned By
            </div>
            <div className="font-semibold text-slate-900 text-sm">{task.assignedByName || '—'}</div>
          </div>
        </div>

        <div className="text-[11px] text-slate-400 flex flex-wrap gap-4 pt-2">
          <span>Created: {formatDateTime(task.createdAt)}</span>
          <span>Last Updated: {formatDateTime(task.updatedAt)}</span>
        </div>
      </div>

      {/* Comments Section */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-sm p-6 sm:p-8 space-y-6">
        <div className="flex items-center justify-between pb-4 border-b border-slate-100">
          <div className="flex items-center gap-2">
            <MessageSquare className="w-5 h-5 text-indigo-600" />
            <h2 className="text-lg font-bold text-slate-900">Comments</h2>
            <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
              {comments.length}
            </span>
          </div>
        </div>

        {/* Add Comment Form */}
        <form onSubmit={handleAddComment} className="space-y-3">
          <div>
            <label className="block text-xs font-semibold text-slate-600 uppercase tracking-wider mb-1">
              Add a comment
            </label>
            <textarea
              required
              maxLength={2000}
              rows={3}
              value={newComment}
              onChange={(e) => setNewComment(e.target.value)}
              placeholder="Write your comment or update here (max 2000 characters)..."
              className="w-full px-3.5 py-2.5 text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 placeholder:text-slate-400 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
            />
          </div>
          <div className="flex items-center justify-between">
            <span className="text-xs text-slate-400">{newComment.length} / 2000</span>
            <button
              type="submit"
              disabled={isSubmittingComment || !newComment.trim()}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-sm transition-colors disabled:opacity-50"
            >
              <Send className="w-3.5 h-3.5" />
              {isSubmittingComment ? 'Posting...' : 'Post Comment'}
            </button>
          </div>
        </form>

        {/* Comments List */}
        <div className="space-y-4 pt-4">
          {comments.length === 0 ? (
            <EmptyState
              icon={MessageSquare}
              title="No comments yet"
              description="Be the first to post a discussion point or progress update on this task."
            />
          ) : (
            comments.map((comment) => {
              const canDeleteComment =
                isAdmin ||
                isManager ||
                comment.userId?.toLowerCase() === user?.id?.toLowerCase();

              return (
                <div
                  key={comment.id}
                  className="p-4 bg-slate-50/80 rounded-xl border border-slate-100 space-y-2 hover:bg-slate-50 transition-colors"
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2.5">
                      <div className="w-7 h-7 rounded-full bg-indigo-100 text-indigo-700 font-bold text-xs flex items-center justify-center">
                        {getInitials(comment.userName)}
                      </div>
                      <div>
                        <span className="text-xs font-semibold text-slate-900">{comment.userName}</span>
                        <span className="text-[11px] text-slate-400 ml-2">{formatDateTime(comment.createdAt)}</span>
                      </div>
                    </div>

                    {canDeleteComment && (
                      <button
                        onClick={() => setDeletingCommentId(comment.id)}
                        className="p-1 text-slate-400 hover:text-rose-600 transition-colors"
                        title="Delete Comment"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    )}
                  </div>
                  <p className="text-sm text-slate-700 whitespace-pre-wrap pl-9">{comment.content}</p>
                </div>
              );
            })
          )}
        </div>
      </div>

      {/* Edit Task Modal */}
      {isEditModalOpen && (
        <TaskFormModal
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          onSuccess={loadTaskData}
          initialData={task}
        />
      )}

      {/* Reassign Task Modal */}
      {isAssignModalOpen && (
        <Modal
          isOpen={isAssignModalOpen}
          onClose={() => setIsAssignModalOpen(false)}
          title="Reassign Task"
          maxWidth="max-w-md"
        >
          <form onSubmit={handleAssignSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-2">
                Select Team Member
              </label>
              <select
                required
                value={selectedAssignee}
                onChange={(e) => setSelectedAssignee(e.target.value)}
                className="w-full px-3.5 py-2.5 text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:ring-2 focus:ring-indigo-500"
              >
                <option value="">Select Member</option>
                {teamMembers.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.name} ({m.role})
                  </option>
                ))}
              </select>
            </div>

            <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsAssignModalOpen(false)}
                className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-lg hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isAssigning || !selectedAssignee}
                className="px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm disabled:opacity-50"
              >
                {isAssigning ? 'Reassigning...' : 'Confirm Assignment'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Delete Task Confirmation */}
      {isDeleteTaskOpen && (
        <ConfirmDialog
          isOpen={isDeleteTaskOpen}
          onClose={() => setIsDeleteTaskOpen(false)}
          onConfirm={handleDeleteTask}
          title="Delete Task"
          message="Are you sure you want to permanently delete this task and all its comments? This cannot be undone."
          confirmLabel="Delete Task"
          isDestructive
          isLoading={isDeletingTask}
        />
      )}

      {/* Delete Comment Confirmation */}
      {deletingCommentId && (
        <ConfirmDialog
          isOpen={!!deletingCommentId}
          onClose={() => setDeletingCommentId(null)}
          onConfirm={handleDeleteComment}
          title="Delete Comment"
          message="Are you sure you want to delete this comment?"
          confirmLabel="Delete Comment"
          isDestructive
          isLoading={isDeletingComment}
        />
      )}
    </div>
  );
}

