import type { Conversation } from '../../types';

interface ConversationListProps {
    conversations: Conversation[];
    activeId: string | null;
    onSelect: (id: string) => void;
}

export default function ConversationList({ conversations, activeId, onSelect }: ConversationListProps) {
    return (
        <div className="flex-1 overflow-y-auto p-2 space-y-1">
        {conversations.length === 0 && (
            <p className="text-gray-500 text-sm text-center mt-4">No conversations yet</p>
        )}
        {conversations.map((conv) => (
            <button
            key={conv.id}
            onClick={() => onSelect(conv.id)}
            className={`w-full text-left p-3 rounded-lg text-sm truncate transition-colors ${
                activeId === conv.id
                ? 'bg-gray-700 text-white'
                : 'text-gray-400 hover:bg-gray-800 hover:text-white'
            }`}
            >
            {conv.title}
            </button>
        ))}
        </div>
    );
}