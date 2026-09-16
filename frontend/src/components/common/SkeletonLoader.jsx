import React from 'react';

export function Skeleton({ className = 'h-4 w-full', rounded = 'rounded-lg' }) {
  return <div className={`skeleton-shimmer ${rounded} ${className}`} />;
}

export function SkeletonStatCard() {
  return (
    <div className="p-5 bg-white border border-slate-200/80 rounded-2xl shadow-sm flex items-center justify-between">
      <div className="space-y-2 flex-1 pr-4">
        <Skeleton className="h-3 w-20" />
        <Skeleton className="h-7 w-12" />
      </div>
      <Skeleton className="w-12 h-12 rounded-xl shrink-0" />
    </div>
  );
}

export function SkeletonTable({ rows = 5, cols = 6 }) {
  return (
    <div className="divide-y divide-slate-100">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="p-4 sm:p-5 flex items-center justify-between gap-4">
          <div className="flex-1 space-y-2">
            <Skeleton className="h-4 w-2/5" />
            <Skeleton className="h-3 w-1/4" />
          </div>
          <Skeleton className="h-6 w-20 rounded-full" />
          <Skeleton className="h-6 w-16 hidden sm:block rounded-full" />
          <Skeleton className="h-4 w-24 hidden md:block" />
          <Skeleton className="h-4 w-12" />
        </div>
      ))}
    </div>
  );
}

export function SkeletonCardGrid({ count = 6 }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="bg-white border border-slate-200/80 rounded-2xl p-6 shadow-sm space-y-4">
          <div className="flex items-center gap-3">
            <Skeleton className="w-10 h-10 rounded-xl shrink-0" />
            <div className="space-y-2 flex-1">
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-3 w-1/3" />
            </div>
          </div>
          <div className="p-4 bg-slate-50 rounded-xl space-y-2">
            <Skeleton className="h-3 w-full" />
            <Skeleton className="h-3 w-4/5" />
          </div>
          <div className="pt-3 border-t border-slate-100 flex justify-between">
            <Skeleton className="h-3 w-16" />
            <Skeleton className="h-4 w-20" />
          </div>
        </div>
      ))}
    </div>
  );
}

