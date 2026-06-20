import type { Document } from '../../types';

interface DocumentListProps {
    documents: Document[];
}

const statusLabels: Record<number, { text: string; color: string }> = {
    0: { text: 'Uploaded', color: 'text-yellow-400' },
    1: { text: 'Processing', color: 'text-blue-400' },
    2: { text: 'Ready', color: 'text-green-400' },
    3: { text: 'Failed', color: 'text-red-400' },
};

export default function DocumentList({ documents }: DocumentListProps) {
    return (
      <div className="flex-1 overflow-y-auto p-2">
        <p className="text-xs text-gray-500 px-2 mb-1">Documents</p>
        {documents.map((doc) => {
          const status = statusLabels[doc.status] ?? { text: 'Unknown', color: 'text-gray-400' };
          return (
            <div key={doc.id} className="px-3 py-2 text-xs text-gray-300 flex justify-between items-center">
              <span className="truncate flex-1">{doc.fileName}</span>
              <span className={`ml-2 ${status.color}`}>{status.text}</span>
            </div>
          );
        })}
      </div>
    );
}