import type { ChatMessage } from '../../types';

interface MessageBubbleProps {
    message: ChatMessage;
}

export default function MessageBubble({ message }: MessageBubbleProps) {
    const isUser = message.role === 'User';

    return (
        <div className={`flex ${isUser ? 'justify-end' : 'justify-start'} mb-4`}>
        <div className={`max-w-2xl px-4 py-3 rounded-2xl text-sm leading-relaxed ${
            isUser
            ? 'bg-blue-600 text-white'
            : 'bg-gray-800 text-gray-100'
        }`}>
            <p className="whitespace-pre-wrap">{message.content}</p>
        </div>
        </div>
    );
}