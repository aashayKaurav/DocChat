interface StreamingMessageProps {
    content: string;
    isStreaming: boolean;
}

export default function StreamingMessage({ content, isStreaming }: StreamingMessageProps) {
    if (!content && !isStreaming) return null;

    return (
      <div className="flex justify-start mb-4">
        <div className="max-w-2xl px-4 py-3 rounded-2xl text-sm leading-relaxed bg-gray-800 text-gray-100">
          <p className="whitespace-pre-wrap">
            {content}
            {isStreaming && <span className="animate-pulse ml-1">|</span>}
          </p>
        </div>
      </div>
    );
}