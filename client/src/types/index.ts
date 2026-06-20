export interface Document {
    id: string;
    fileName: string;
    contentType: string;
    fileSize: number;
    status: number;
    chunkCount: number;
    createdAt: string;
    processedAt: string | null;
  }

  export interface Conversation {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
  }

  export interface ChatMessage {
    id: string;
    conversationId: string;
    role: 'User' | 'Assistant';
    content: string;
    sourceChunkIds: string | null;
    createdAt: string;
  }

  export interface SourceCitation {
    documentId: string;
    fileName: string;
    chunkIndex: number;
    score: number;
    content: string;
  }

  export interface AskQuestionResult {
    messageId: string;
    answer: string;
    citations: SourceCitation[];
  }