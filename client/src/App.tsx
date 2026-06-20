import { useState, useEffect, useCallback } from 'react';
import type { ChatMessage, Conversation, Document, SourceCitation } from './types';
import { useSignalR } from './hooks/useSignalR';
import apiClient from './api/apiClient';
import AppLayout from './components/Layout/AppLayout';
import NewChatButton from './components/Sidebar/NewChatButton';
import ConversationList from './components/Sidebar/ConversationList';
import UploadPanel from './components/Documents/UploadPanel';
import DocumentList from './components/Documents/DocumentList';
import ChatWindow from './components/Chat/ChatWindow';

function App() {
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [activeConversationId, setActiveConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [documents, setDocuments] = useState<Document[]>([]);
  const [streamingContent, setStreamingContent] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [citations, setCitations] = useState<SourceCitation[]>([]);

  const { isConnected, askQuestion } = useSignalR();

  // Load conversations and documents on mount
  useEffect(() => {
    loadConversations();
    loadDocuments();
  }, []);

  // Load messages when active conversation changes
  useEffect(() => {
    if (activeConversationId) {
      loadMessages(activeConversationId);
    } else {
      setMessages([]);
    }
  }, [activeConversationId]);

  const loadConversations = async () => {
    try {
      const res = await apiClient.get('/api/chat/conversations');
      setConversations(res.data);
    } catch (err) {
      console.error('Failed to load conversations:', err);
    }
  };

  const loadMessages = async (conversationId: string) => {
    try {
      const res = await apiClient.get(`/api/chat/conversations/${conversationId}/messages`);
      setMessages(res.data);
    } catch (err) {
      console.error('Failed to load messages:', err);
    }
  };

  const loadDocuments = async () => {
    try {
      const res = await apiClient.get('/api/documents');
      setDocuments(res.data);
    } catch (err) {
      console.error('Failed to load documents:', err);
    }
  };

  const handleNewChat = () => {
    setActiveConversationId(null);
    setMessages([]);
    setCitations([]);
    setStreamingContent('');
  };

  const handleSendMessage = useCallback((question: string) => {
    const conversationId = activeConversationId || crypto.randomUUID();

    // Add user message to UI immediately
    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      conversationId,
      role: 'User',
      content: question,
      sourceChunkIds: null,
      createdAt: new Date().toISOString(),
    };
    setMessages(prev => [...prev, userMessage]);
    setActiveConversationId(conversationId);
    setIsStreaming(true);
    setStreamingContent('');
    setCitations([]);

    askQuestion(
      conversationId,
      question,
      // onToken
      (token) => {
        setStreamingContent(prev => prev + token);
      },
      // onCitations
      (newCitations) => {
        setCitations(newCitations);
      },
      // onComplete
      () => {
        setIsStreaming(false);
        // Reload messages to get the saved assistant message
        loadMessages(conversationId);
        loadConversations();
        setStreamingContent('');
        setCitations([]);
      },
      // onError
      (error) => {
        setIsStreaming(false);
        setStreamingContent(`Error: ${error}`);
      }
    );
  }, [activeConversationId, askQuestion]);

  return (
    <AppLayout
      sidebar={
        <>
          <NewChatButton onClick={handleNewChat} />
          <UploadPanel onUploadComplete={loadDocuments} />
          <DocumentList documents={documents} />
          <div className="border-t border-gray-800 p-3">
            <p className="text-xs text-gray-500">
              {isConnected ? '🟢 Connected' : '🔴 Disconnected'}
            </p>
          </div>
          <ConversationList
            conversations={conversations}
            activeId={activeConversationId}
            onSelect={setActiveConversationId}
          />
        </>
      }
      main={
        <ChatWindow
          messages={messages}
          streamingContent={streamingContent}
          isStreaming={isStreaming}
          citations={citations}
          onSendMessage={handleSendMessage}
        />
      }
    />
  );
}

export default App;