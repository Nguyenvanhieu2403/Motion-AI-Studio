import { Injectable } from '@angular/core'; import { HttpClient } from '@angular/common/http'; import { Observable } from 'rxjs'; import { environment } from '../environments/environment'; import { VideoJob } from './models';
@Injectable({ providedIn: 'root' })
export class VideoJobService {
  private readonly endpoint = `${environment.apiUrl}/video-jobs`;
  constructor(private readonly http: HttpClient) {}
  create(form: FormData): Observable<VideoJob> { return this.http.post<VideoJob>(this.endpoint, form); }
  get(id: string): Observable<VideoJob> { return this.http.get<VideoJob>(`${this.endpoint}/${id}`); }
}
