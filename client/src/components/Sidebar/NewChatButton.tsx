interface NewChatButtonProps {
    onClick: () => void;
}

export default function NewChatButton({ onClick }: NewChatButtonProps) {
    return (
        <button
        onClick={onClick}
        className="m-3 p-3 bg-blue-600 hover:bg-blue-700 rounded-lg text-sm font-medium transition-colors"
        >
        + New Chat
        </button>
    );
}