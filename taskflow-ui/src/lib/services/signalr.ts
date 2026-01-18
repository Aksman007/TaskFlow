/* eslint-disable @typescript-eslint/no-explicit-any */
import * as signalR from '@microsoft/signalr';
import { Task, Comment } from '../types';

type EventHandler = (...args: any[]) => void;

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private handlers = new Map<string, Set<EventHandler>>();

  async connect(token: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    const hubUrl = process.env.NEXT_PUBLIC_HUB_URL || 'http://localhost:5030/hubs/tasks';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupHandlers();
    this.setupLifecycleEvents();

    try {
      await this.connection.start();
      console.log('✅ SignalR connected');
    } catch (err) {
      console.error('❌ SignalR connection error:', err);
    }
  }

  private setupHandlers(): void {
    if (!this.connection) return;

    this.connection.on('TaskCreated', (task: Task) => {
      this.trigger('TaskCreated', task);
    });

    this.connection.on('TaskUpdated', (task: Task) => {
      this.trigger('TaskUpdated', task);
    });

    this.connection.on('TaskDeleted', (taskId: string) => {
      this.trigger('TaskDeleted', taskId);
    });

    this.connection.on('CommentAdded', (taskId: string, comment: Comment) => {
      this.trigger('CommentAdded', { taskId, comment });
    });
  }

  private setupLifecycleEvents(): void {
    if (!this.connection) return;

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
    });
  }

  async joinProject(projectId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinProject', projectId);
    }
  }

  async leaveProject(projectId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveProject', projectId);
    }
  }

  on(event: string, handler: EventHandler): void {
    if (!this.handlers.has(event)) {
      this.handlers.set(event, new Set());
    }
    this.handlers.get(event)!.add(handler);
  }

  off(event: string, handler: EventHandler): void {
    this.handlers.get(event)?.delete(handler);
  }

  private trigger(event: string, data: any): void {
    this.handlers.get(event)?.forEach((handler) => handler(data));
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRService = new SignalRService();