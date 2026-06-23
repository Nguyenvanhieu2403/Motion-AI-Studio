export type JobStatus = 'Pending' | 'Analyzing' | 'PromptGenerated' | 'Rendering' | 'Completed' | 'Failed';
export interface VideoJob { id: string; status: JobStatus; positivePrompt?: string; negativePrompt?: string; errorMessage?: string; createdAt: string; updatedAt: string; videoUrl?: string; }
