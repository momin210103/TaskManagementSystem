import React from 'react';

export function StatusBadge({ status }) {
  const normalized = status?.toLowerCase() || '';

  const styles = {
    todo: 'bg-blue-50 text-blue-700 border-blue-200',
    inprogress: 'bg-amber-50 text-amber-700 border-amber-200',
    done: 'bg-emerald-50 text-emerald-700 border-emerald-200'
  };

  const labels = {
    todo: 'To Do',
    inprogress: 'In Progress',
    done: 'Done'
  };

  const badgeStyle = styles[normalized] || 'bg-slate-50 text-slate-700 border-slate-200';
  const label = labels[normalized] || status || 'Unknown';

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border ${badgeStyle}`}>
      {label}
    </span>
  );
}

