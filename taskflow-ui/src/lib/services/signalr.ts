import * as signalR from '@microsoft/signalr';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private eventHandlers: Map<string, Function[]> = new Map();

  async connect() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      console.log('SignalR already connected');
      return;
    }

    const hubUrl = process.env.NEXT_PUBLIC_HUB_URL || 'http://localhost:5030/hubs/tasks';

    console.log('Connecting to SignalR hub:', hubUrl);

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        // Use cookie-based auth — no access token factory needed
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set up lifecycle handlers
    this.connection.onclose((error) => {
      console.log('SignalR connection closed', error);
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting...', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected with connection ID:', connectionId);
    });

    try {
      await this.connection.start();
      console.log('SignalR connected successfully');
    } catch (error) {
      console.error('SignalR connection failed:', error);
      throw error;
    }
  }

  async disconnect() {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('SignalR disconnected');
      } catch (error) {
        console.error('Error disconnecting SignalR:', error);
      }
      this.connection = null;
    }
  }

  async joinProject(projectId: string) {
    if (!this.connection) {
      throw new Error('SignalR not connected');
    }

    try {
      await this.connection.invoke('JoinProject', projectId);
      console.log('Joined project group:', projectId);
    } catch (error) {
      console.error('Failed to join project:', error);
      throw error;
    }
  }

  async leaveProject(projectId: string) {
    if (!this.connection) {
      return;
    }

    try {
      await this.connection.invoke('LeaveProject', projectId);
      console.log('Left project group:', projectId);
    } catch (error) {
      console.error('Failed to leave project:', error);
    }
  }

  on(eventName: string, handler: Function) {
    if (!this.connection) {
      console.warn('Cannot register handler, SignalR not connected');
      return;
    }

    // Normalize event name to lowercase
    const normalizedEventName = eventName.toLowerCase();

    console.log(`Registering handler for event: ${eventName} (normalized: ${normalizedEventName})`);

    // Store handler
    if (!this.eventHandlers.has(normalizedEventName)) {
      this.eventHandlers.set(normalizedEventName, []);
    }
    this.eventHandlers.get(normalizedEventName)!.push(handler);

    // Register with SignalR using the normalized name
    this.connection.on(normalizedEventName, (...args: any[]) => {
      console.log(`SignalR event received: ${normalizedEventName}`, args);
      handler(...args);
    });
  }

  off(eventName: string, handler: Function) {
    if (!this.connection) {
      return;
    }

    const normalizedEventName = eventName.toLowerCase();

    console.log(`Unregistering handler for event: ${eventName} (normalized: ${normalizedEventName})`);

    // Remove from stored handlers
    const handlers = this.eventHandlers.get(normalizedEventName);
    if (handlers) {
      const index = handlers.indexOf(handler);
      if (index > -1) {
        handlers.splice(index, 1);
      }
    }

    // Remove from SignalR
    this.connection.off(normalizedEventName, handler as any);
  }

  async sendUserTyping(projectId: string, taskId: string) {
    if (!this.connection) {
      throw new Error('SignalR not connected');
    }

    try {
      await this.connection.invoke('UserTyping', projectId, taskId);
    } catch (error) {
      console.error('Failed to send typing notification:', error);
    }
  }

  async sendUserStoppedTyping(projectId: string, taskId: string) {
    if (!this.connection) {
      throw new Error('SignalR not connected');
    }

    try {
      await this.connection.invoke('UserStoppedTyping', projectId, taskId);
    } catch (error) {
      console.error('Failed to send stopped typing notification:', error);
    }
  }
}

export const signalRService = new SignalRService();
