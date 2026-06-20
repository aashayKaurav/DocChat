import { useState, useRef, useEffect } from 'react';
import type { ChatMessage as ChatMessageType, SourceCitation } from '../../types';
import MessageBubble from './MessageBubble';
import StreamingMessage from './StreamingMessage';
import SourceCitationList from './SourceCitation';

interface ChatWindowProps {
    messages: ChatMessageType[];
    streamingContent: string;
    isStreaming: boolean;
    citations: SourceCitation[];
    onSendMessage: (question: string) => void;
}

export default function ChatWindow({ messages, streamingContent, isStreaming, citations, onSendMessage }: ChatWindowProps) {
    const [input, setInput] = useState('');
    const messagesEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
      messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, streamingContent]);

    const handleSubmit = (e: React.SubmitEvent<HTMLFormElement>) => {
      e.preventDefault();
      if (!input.trim() || isStreaming) return;
      onSendMessage(input.trim());
      setInput('');
    };

    return (
      <div className="flex-1 flex flex-col">
        {/* Messages area */}
        <div className="flex-1 overflow-y-auto p-6">
          {messages.length === 0 && !isStreaming && (
            <div className="flex items-center justify-center h-full">
              <div className="text-center text-gray-500">
                <h2 className="text-2xl font-semibold mb-2">DocChat</h2>
                <p className="text-sm">Upload documents and ask questions about them</p>
              </div>
            </div>
          )}

          {messages.map((msg) => (
            <MessageBubble key={msg.id} message={msg} />
          ))}

          {citations.length > 0 && isStreaming && <SourceCitationList citations={citations} />}
          <StreamingMessage content={streamingContent} isStreaming={isStreaming} />

          <div ref={messagesEndRef} />
        </div>

        {/* Input area */}
        <form onSubmit={handleSubmit} className="p-4 border-t border-gray-800">
          <div className="flex gap-3 max-w-4xl mx-auto">
            <input
              type="text"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Ask a question about your documents..."
              className="flex-1 bg-gray-800 text-white border border-gray-700 rounded-xl px-4 py-3 text-sm focus:outline-none focus:border-blue-500 placeholder-gray-500"
              disabled={isStreaming}
            />
            <button
              type="submit"
              disabled={isStreaming || !input.trim()}
              className="bg-blue-600 hover:bg-blue-700 disabled:bg-gray-700 disabled:text-gray-500 text-white px-6 py-3 rounded-xl text-sm font-medium transition-colors"
            >
              Send
            </button>
          </div>
        </form>
      </div>
    );
}