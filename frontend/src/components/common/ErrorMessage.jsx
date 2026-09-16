import React from 'react';
import { AlertCircle, X } from 'lucide-react';

export function ErrorMessage({ message, onDismiss, retry }) {
  if (!message) return null;

  return (
    <div className="flex items-start justify-between gap-3 p-3.5 sm:p-4 mb-4 text-xs sm:text-sm text-rose-800 bg-rose-50 border border-rose-200 rounded-xl shadow-sm animate-fade-in">
      <div className="flex items-start gap-3 min-w-0">
        <AlertCircle className="w-5 h-5 text-rose-600 shrink-0 mt-0.5" />
        <div className="min-w-0">
          <p className="font-semibold break-words">{message}</p>
          {retry && (
            <button
              onClick={retry}
              className="mt-1.5 text-xs font-bold text-rose-700 underline hover:text-rose-900 block"
            >
              Try again &rarr;
            </button>
          )}
        </div>
      </div>
      {onDismiss && (
        <button
          onClick={onDismiss}
          className="text-rose-500 hover:text-rose-700 p-1 rounded-lg hover:bg-rose-100 transition-colors shrink-0"
          aria-label="Dismiss error"
        >
          <X className="w-4 h-4" />
        </button>
      )}
    </div>
  );
}
