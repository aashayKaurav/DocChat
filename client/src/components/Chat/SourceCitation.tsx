import type { SourceCitation as SourceCitationType } from '../../types';

interface SourceCitationProps {
    citations: SourceCitationType[];
}

export default function SourceCitationList({ citations }: SourceCitationProps) {
    if (citations.length === 0) return null;

    return (
      <div className="mb-4 p-3 bg-gray-800 rounded-lg border border-gray-700">
        <p className="text-xs text-gray-400 mb-2 font-medium">Sources:</p>
        <div className="space-y-2">
          {citations.map((c, i) => (
            <div key={i} className="text-xs text-gray-300 bg-gray-900 p-2 rounded">
              <span className="text-blue-400 font-medium">{c.fileName}</span>
              <span className="text-gray-500 ml-2">Chunk {c.chunkIndex} ({(c.score * 100).toFixed(0)}% match)</span>
              <p className="mt-1 text-gray-400 truncate">{c.content}</p>
            </div>
          ))}
        </div>
      </div>
    );
}