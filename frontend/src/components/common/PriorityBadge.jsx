import React from 'react';

export function PriorityBadge({ priority }) {
  const normalized = priority?.toLowerCase() || '';

  const styles = {
    low: 'bg-slate-100 text-slate-700 border-slate-200',
    medium: 'bg-amber-100 text-amber-800 border-amber-300',
    high: 'bg-rose-100 text-rose-800 border-rose-300'
  };

  const badgeStyle = styles[normalized] || 'bg-slate-100 text-slate-700 border-slate-200';

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold uppercase tracking-wider border ${badgeStyle}`}>
      {priority || 'None'}
    </span>
  );
}

