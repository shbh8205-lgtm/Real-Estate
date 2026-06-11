import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Property } from '../models/property.model';

export interface ChatReply {
  reply: string;
  suggestedProperties: Property[];
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly API = 'https://localhost:7144/api/chat';
  private http = inject(HttpClient);

  // מזהה שיחה ייחודי לכל דפדפן/משתמש, כדי שלא ישתפו את אותה היסטוריית צ'אט בשרת.
  private readonly conversationId = this.getOrCreateConversationId();

  send(message: string): Observable<ChatReply> {
    return this.http.post<ChatReply>(this.API, {
      message,
      conversationId: this.conversationId,
    });
  }

  private getOrCreateConversationId(): string {
    const KEY = 'chat_conversation_id';
    let id = localStorage.getItem(KEY);
    if (!id) {
      id =
        typeof crypto !== 'undefined' && 'randomUUID' in crypto
          ? crypto.randomUUID()
          : `conv_${Date.now()}_${Math.random().toString(36).slice(2)}`;
      localStorage.setItem(KEY, id);
    }
    return id;
  }
}
