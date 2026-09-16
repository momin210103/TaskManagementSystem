import React from 'react';
import { AlertCircle, X } from 'lucide-react';

export function ErrorMessage({ message, onDismiss, retry }) {
  if (!message) return null;

  return (
    <div className="flex items-start justify-between gap-3 p-4 mb-4 text-sm text-rose-800 bg-rose-50 border border-rose-200 rounded-lg shadow-sm">
      <div className="flex items-start gap-3">
        <AlertCircle className="w-5 h-5 text-rose-600 shrink-0 mt-0.5" />
        <div>
          <p className="font-medium">{message}</p>
          {retry && (
            <button
              onClick={retry}
              className="mt-2 text-xs font-semibold text-rose-700 underline hover:text-rose-900"
            >
              Try again
            </button>
          )}
        </div>
      </div>
      {onDismiss && (
        <button
          onClick={onDismiss}
          className="text-rose-500 hover:text-rose-700 p-1 transition-colors"
          aria-label="Dismiss error"
        >
          <X className="w-4 h-4" />
        </button>
      )}
    </div>
  );
}

