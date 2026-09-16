import React from 'react';
import { Inbox } from 'lucide-react';

export function EmptyState({
  icon: Icon = Inbox,
  title = 'No items found',
  description = 'There are no records to display at this time.',
  actionLabel,
  onAction
}) {
  return (
    <div className="flex flex-col items-center justify-center p-8 sm:p-12 text-center border-2 border-dashed border-slate-200 rounded-2xl bg-slate-50/50 m-3 sm:m-4 animate-fade-in">
      <div className="p-3.5 sm:p-4 bg-white border border-slate-200 rounded-2xl shadow-sm mb-3.5 sm:mb-4">
        <Icon className="w-7 h-7 sm:w-8 sm:h-8 text-slate-400" />
      </div>
      <h3 className="text-sm sm:text-base font-bold text-slate-900">{title}</h3>
      <p className="max-w-sm mt-1 text-xs sm:text-sm text-slate-500">{description}</p>
      {actionLabel && onAction && (
        <button
          onClick={onAction}
          className="mt-5 inline-flex items-center px-4 py-2 text-xs sm:text-sm font-bold text-white bg-indigo-600 rounded-xl shadow-sm hover:bg-indigo-700 transition-colors focus:outline-none focus:ring-2 focus:ring-indigo-500"
        >
          {actionLabel}
        </button>
      )}
    </div>
  );
}
