export class AuditraksApiClient {
    baseUrl;
    apiKey;
    accessToken;
    email;
    password;
    constructor(config) {
        this.baseUrl = config.baseUrl.replace(/\/$/, '');
        this.apiKey = config.apiKey;
        this.email = config.email;
        this.password = config.password;
    }
    async login() {
        if (!this.email || !this.password)
            return;
        const res = await fetch(`${this.baseUrl}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: this.email, password: this.password }),
        });
        if (!res.ok)
            throw new Error(`Login failed: ${res.status}`);
        const data = await res.json();
        this.accessToken = data.accessToken;
    }
    async getHeaders() {
        const headers = { 'Content-Type': 'application/json' };
        if (this.apiKey) {
            headers['X-API-Key'] = this.apiKey;
        }
        else if (this.accessToken) {
            headers['Authorization'] = `Bearer ${this.accessToken}`;
        }
        return headers;
    }
    async request(method, path, body) {
        const headers = await this.getHeaders();
        const url = `${this.baseUrl}${path}`;
        const res = await fetch(url, {
            method,
            headers,
            body: body ? JSON.stringify(body) : undefined,
        });
        if (res.status === 401 && this.email && this.password) {
            await this.login();
            const retryHeaders = await this.getHeaders();
            const retry = await fetch(url, {
                method,
                headers: retryHeaders,
                body: body ? JSON.stringify(body) : undefined,
            });
            if (!retry.ok) {
                const err = await retry.json().catch(() => ({ error: retry.statusText }));
                throw new Error(`${retry.status}: ${err.error}`);
            }
            return retry.json();
        }
        if (!res.ok) {
            const err = await res.json().catch(() => ({ error: res.statusText }));
            throw new Error(`${res.status}: ${err.error}`);
        }
        if (res.status === 204)
            return {};
        return res.json();
    }
    get(path) { return this.request('GET', path); }
    post(path, body) { return this.request('POST', path, body); }
    patch(path, body) { return this.request('PATCH', path, body); }
    delete(path) { return this.request('DELETE', path); }
}
