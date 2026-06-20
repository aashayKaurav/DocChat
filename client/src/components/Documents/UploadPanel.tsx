import { useRef, useState } from 'react';
import apiClient from '../../api/apiClient';

interface UploadPanelProps {
    onUploadComplete: () => void;
}

export default function UploadPanel({ onUploadComplete }: UploadPanelProps) {
    const [isUploading, setIsUploading] = useState(false);
    const [dragOver, setDragOver] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);

    const uploadFile = async (file: File) => {
      setIsUploading(true);
      try {
        const formData = new FormData();
        formData.append('file', file);
        await apiClient.post('/api/documents/upload', formData);
        onUploadComplete();
      } catch (err) {
        console.error('Upload failed:', err);
      } finally {
        setIsUploading(false);
      }
    };

    const handleDrop = (e: React.DragEvent) => {
      e.preventDefault();
      setDragOver(false);
      const file = e.dataTransfer.files[0];
      if (file) uploadFile(file);
    };

    return (
      <div
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        className={`m-3 p-4 border-2 border-dashed rounded-lg text-center cursor-pointer transition-colors text-xs ${
          dragOver ? 'border-blue-500 bg-blue-500/10' : 'border-gray-700 hover:border-gray-500'
        }`}
      >
        <input
          ref={fileInputRef}
          type="file"
          accept=".pdf,.txt"
          className="hidden"
          onChange={(e) => {
            const file = e.target.files?.[0];
            if (file) uploadFile(file);
          }}
        />
        {isUploading ? (
          <p className="text-blue-400">Uploading...</p>
        ) : (
          <p className="text-gray-400">Drop PDF/TXT here or click to upload</p>
        )}
      </div>
    );
}