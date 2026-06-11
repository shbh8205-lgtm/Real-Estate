import { AfterViewChecked, Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ChatService } from '../../../core/services/chat.service';
import { Property } from '../../../core/models/property.model';

interface Message {
  role: 'user' | 'assistant';
  text: string;
  properties?: Property[];
}

@Component({
  selector: 'app-chat',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './chat.html',
  styleUrl: './chat.css',
})
export class ChatComponent implements AfterViewChecked {
  private fb = inject(FormBuilder);
  private chat = inject(ChatService);

  @ViewChild('scroller') scroller?: ElementRef<HTMLDivElement>;

  messages = signal<Message[]>([
    { role: 'assistant', text: 'שלום! תאר לי איזה נכס אתה מחפש — אני אעזור לך למצוא התאמות מתוך הרשימה שלנו.' }
  ]);
  isSending = signal<boolean>(false);

  form = this.fb.nonNullable.group({
    message: ['', [Validators.required, Validators.minLength(1)]]
  });

  private shouldScroll = false;

  send() {
    if (this.form.invalid || this.isSending()) return;
    const text = this.form.controls.message.value.trim();
    if (!text) return;

    this.messages.update(m => [...m, { role: 'user', text }]);
    this.form.controls.message.reset('');
    this.isSending.set(true);
    this.shouldScroll = true;

    this.chat.send(text).subscribe({
      next: (res) => {
        this.messages.update(m => [...m, {
          role: 'assistant',
          text: res.reply,
          properties: res.suggestedProperties
        }]);
        this.isSending.set(false);
        this.shouldScroll = true;
      },
      error: (err) => {
        this.messages.update(m => [...m, {
          role: 'assistant',
          text: 'שגיאה בתקשורת עם השרת. נסה שוב.'
        }]);
        this.isSending.set(false);
        this.shouldScroll = true;
        console.error('Chat error:', err);
      }
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.scroller) {
      this.scroller.nativeElement.scrollTop = this.scroller.nativeElement.scrollHeight;
      this.shouldScroll = false;
    }
  }
}
