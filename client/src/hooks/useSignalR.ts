import { useEffect, useRef, useState, useCallback } from 'react';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';

export function useSignalR() {
  const connectionRef = useRef<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('http://localhost:5123/hubs/chat')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    connection.onclose(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));

    connection.start()
      .then(() => {
        setIsConnected(true);
        connectionRef.current = connection;
      })
      .catch(err => console.error('SignalR connection failed:', err));

    return () => {
      connection.stop();
    };
  }, []);

  const askQuestion = useCallback((
    conversationId: string,
    question: string,
    onToken: (token: string) => void,
    onCitations: (citations: any[]) => void,
    onComplete: () => void,
    onError: (error: string) => void
  ) => {
    const connection = connectionRef.current;
    if (!connection) return;

    // Register event handlers
    connection.off('ReceiveToken');
    connection.off('ReceiveCitations');
    connection.off('StreamComplete');
    connection.off('ReceiveError');

    connection.on('ReceiveToken', onToken);
    connection.on('ReceiveCitations', onCitations);
    connection.on('StreamComplete', onComplete);
    connection.on('ReceiveError', onError);

    // Invoke the hub method
    connection.invoke('AskQuestion', conversationId, question)
      .catch(err => onError(err.message));
  }, []);

  return { isConnected, askQuestion };
}