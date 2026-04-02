export function createMockApiClient() {
    let lastCall = null;
    let mockResponse = {};
    const mock = {
        _lastCall: null,
        _mockResponse: {},
        _setMockResponse(response) {
            mockResponse = response;
            mock._mockResponse = response;
        },
        async get(path) {
            lastCall = { method: 'GET', path };
            mock._lastCall = lastCall;
            return mockResponse;
        },
        async post(path, body) {
            lastCall = { method: 'POST', path, body };
            mock._lastCall = lastCall;
            return mockResponse;
        },
        async patch(path, body) {
            lastCall = { method: 'PATCH', path, body };
            mock._lastCall = lastCall;
            return mockResponse;
        },
        async delete(path) {
            lastCall = { method: 'DELETE', path };
            mock._lastCall = lastCall;
            return mockResponse;
        },
        async login() { },
        async request() { return mockResponse; },
    };
    return mock;
}
