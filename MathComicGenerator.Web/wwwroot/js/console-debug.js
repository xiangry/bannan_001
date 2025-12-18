/**
 * Console Debug System for Math Comic Generator
 * Provides comprehensive UTF-8 formatted debugging output
 */

class ConsoleDebugger {
    constructor() {
        this.isEnabled = false; // 禁用调试日志以提高性能
        this.logLevel = 'ERROR'; // 只记录错误日志
        this.sessionId = this.generateSessionId();
        this.startTime = new Date();
        
        // Initialize console with UTF-8 support
        this.initializeConsole();
        
        // Log system initialization
        this.logInfo('Console Debug System Initialized', {
            sessionId: this.sessionId,
            startTime: this.startTime.toISOString(),
            userAgent: navigator.userAgent,
            language: navigator.language
        });
    }

    generateSessionId() {
        return 'session_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    initializeConsole() {
        // Ensure console supports UTF-8
        if (typeof console !== 'undefined') {
            console.log('%c🎯 Math Comic Generator Debug Console', 
                'color: #4CAF50; font-size: 16px; font-weight: bold;');
            console.log('%c📊 UTF-8 encoding enabled for Chinese characters', 
                'color: #2196F3; font-size: 12px;');
        }
    }

    formatMessage(level, category, message, data = null) {
        const timestamp = new Date().toISOString();
        const elapsed = Date.now() - this.startTime.getTime();
        
        const logEntry = {
            timestamp,
            elapsed: `${elapsed}ms`,
            level,
            category,
            message,
            sessionId: this.sessionId
        };

        if (data) {
            logEntry.data = data;
        }

        return logEntry;
    }

    logDebug(message, data = null, category = 'DEBUG') {
        if (!this.isEnabled) return;
        // 快速返回，避免不必要的处理
    }

    logInfo(message, data = null, category = 'INFO') {
        if (!this.isEnabled) return;
        // 快速返回，避免不必要的处理
    }

    logWarn(message, data = null, category = 'WARN') {
        if (!this.isEnabled) return;
        // 快速返回，避免不必要的处理
    }

    logError(message, error = null, category = 'ERROR') {
        // 只记录错误日志，用于调试重要问题
        const timestamp = new Date().toISOString();
        console.error('❌ [ERROR]', timestamp, `[${category}]`, message, error || '');
    }

    // User Interaction Logging - 优化为快速返回
    logUserInput(inputType, value, element = null) {
        if (!this.isEnabled) return;
    }

    logUserClick(elementInfo, coordinates = null) {
        if (!this.isEnabled) return;
    }

    logUserSelection(selectionType, selectedValue, options = null) {
        if (!this.isEnabled) return;
    }

    // API Request/Response Logging - 只记录错误
    logApiRequest(method, url, requestData = null, headers = null) {
        if (!this.isEnabled) return;
    }

    logApiResponse(method, url, status, responseData = null, duration = null) {
        // 只记录API错误响应
        if (status >= 400) {
            console.error('❌ API Error Response:', method, url, status, duration ? `${duration}ms` : '');
        }
    }

    logApiError(method, url, error, requestData = null) {
        // 始终记录API错误
        console.error('❌ API Error:', method, url, error.message || error);
    }

    // Application State Logging - 禁用
    logStateChange(component, oldState, newState) {
        if (!this.isEnabled) return;
    }

    logComponentLifecycle(component, lifecycle, data = null) {
        if (!this.isEnabled) return;
    }

    // Form Validation Logging - 只记录验证错误
    logValidation(formName, fieldName, isValid, errorMessage = null) {
        if (!isValid && errorMessage) {
            console.warn('⚠️ Validation Error:', formName, fieldName, errorMessage);
        }
    }

    // Performance Logging - 只记录慢操作
    logPerformance(operation, duration, details = null) {
        if (duration > 2000) { // 只记录超过2秒的操作
            console.warn('⚠️ Slow Operation:', operation, `${duration}ms`, details);
        }
    }

    // Comic Generation Specific Logging - 只记录关键步骤
    logComicGeneration(step, status, data = null) {
        if (status === 'error' || status === 'failed') {
            console.error('❌ Comic Generation Error:', step, status, data);
        }
    }

    logPromptGeneration(mathConcept, options, result = null) {
        if (!this.isEnabled) return;
    }

    // Utility Methods
    logTable(title, data) {
        if (!this.isEnabled) return;
        
        console.group(`📋 ${title}`);
        console.table(data);
        console.groupEnd();
    }

    logGroup(title, callback) {
        if (!this.isEnabled) return;
        
        console.group(`📁 ${title}`);
        try {
            callback();
        } finally {
            console.groupEnd();
        }
    }

    // Configuration Methods
    enable() {
        this.isEnabled = true;
        this.logInfo('Console debugging enabled');
    }

    disable() {
        this.logInfo('Console debugging disabled');
        this.isEnabled = false;
    }

    setLogLevel(level) {
        this.logLevel = level;
        this.logInfo(`Log level set to: ${level}`);
    }

    // Export logs for debugging
    exportLogs() {
        const logs = {
            sessionId: this.sessionId,
            startTime: this.startTime,
            exportTime: new Date(),
            userAgent: navigator.userAgent,
            url: window.location.href
        };
        
        console.log('📤 Exporting debug session:', logs);
        return logs;
    }
}

// Initialize global debugger instance
window.debugger = new ConsoleDebugger();

// Expose common logging functions globally
window.logDebug = (message, data, category) => window.debugger.logDebug(message, data, category);
window.logInfo = (message, data, category) => window.debugger.logInfo(message, data, category);
window.logWarn = (message, data, category) => window.debugger.logWarn(message, data, category);
window.logError = (message, error, category) => window.debugger.logError(message, error, category);

// User interaction helpers
window.logUserInput = (type, value, element) => window.debugger.logUserInput(type, value, element);
window.logUserClick = (info, coords) => window.debugger.logUserClick(info, coords);
window.logUserSelection = (type, value, options) => window.debugger.logUserSelection(type, value, options);

// API helpers
window.logApiRequest = (method, url, data, headers) => window.debugger.logApiRequest(method, url, data, headers);
window.logApiResponse = (method, url, status, data, duration) => window.debugger.logApiResponse(method, url, status, data, duration);
window.logApiError = (method, url, error, data) => window.debugger.logApiError(method, url, error, data);

// Application helpers
window.logStateChange = (component, oldState, newState) => window.debugger.logStateChange(component, oldState, newState);
window.logValidation = (form, field, valid, error) => window.debugger.logValidation(form, field, valid, error);
window.logPerformance = (operation, duration, details) => window.debugger.logPerformance(operation, duration, details);

// Comic generation helpers
window.logComicGeneration = (step, status, data) => window.debugger.logComicGeneration(step, status, data);
window.logPromptGeneration = (concept, options, result) => window.debugger.logPromptGeneration(concept, options, result);

console.log('🚀 Console Debug System loaded successfully!');