export interface ApiClientConfig {
    baseUrl: string;
    apiKey?: string;
    email?: string;
    password?: string;
}
export declare class AuditraksApiClient {
    private baseUrl;
    private apiKey?;
    private accessToken?;
    private email?;
    private password?;
    constructor(config: ApiClientConfig);
    login(): Promise<void>;
    private getHeaders;
    request<T>(method: string, path: string, body?: unknown): Promise<T>;
    get<T>(path: string): Promise<T>;
    post<T>(path: string, body?: unknown): Promise<T>;
    patch<T>(path: string, body?: unknown): Promise<T>;
    delete<T>(path: string): Promise<T>;
}
