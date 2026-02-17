export interface User {
  id: string;
  email: string;
  fullName: string;
  createdAt: string;
}

export interface AuthResult {
  success: boolean;
  token?: string;
  user?: User;
  error?: string;
  expiresAt?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export enum TaskStatus {
  Todo = 0,
  InProgress = 1,
  Done = 2,
}

export enum TaskPriority {
  Low = 0,
  Medium = 1,
  High = 2,
}

export enum ProjectRole {
    Viewer = 0,
    Member = 1,
    Admin = 2,
}

export interface Task {
  id: string;
  title: string;
  description: string;
  projectId: string;
  assignedToId?: string;
  assignedToName?: string;
  status: TaskStatus;
  priority: TaskPriority;
  createdAt: string;
  dueDate?: string;
}

export interface Project {
  id: string;
  name: string;
  description: string;
  ownerId: string;
  ownerName: string;
  createdAt: string;
  memberCount: number;
  taskCount: number;
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  projectId: string;
  assignedToId?: string;
  priority: TaskPriority;
  dueDate?: string;
}

export interface UpdateTaskRequest {
  title: string;
  description?: string;
  status: TaskStatus;
  priority: TaskPriority;
  assignedToId?: string;
  dueDate?: string;
}

export interface Comment {
  id: string;
  taskId: string;
  userId: string;
  userName: string;
  content: string;
  createdAt: string;
  updatedAt?: string;
}

export interface AddCommentRequest {
  taskId: string;
  projectId: string;
  content: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  skip: number;
  take: number;
  hasMore: boolean;
}
