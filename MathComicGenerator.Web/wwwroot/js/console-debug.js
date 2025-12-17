/**
 * Console Debug System for Math Comic Generator
 * Provides comprehensive UTF-8 formatted debugging output
 */

class ConsoleDebugger {
    constructor() {
        this.isEnabled = true;
        this.logLevel = 'DEBUG'; // DEBUG, INFO, WARN, ERROR
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
        
        const logEntry = this.formatMessage('DEBUG', category, message, data);
        console.debug('🔍 [DEBUG]', logEntry.timestamp, `[${category}]`, message, data || '');
    }

    logInfo(message, data = null, category = 'INFO') {
        if (!this.isEnabled) return;
        
        const logEntry = this.formatMessage('INFO', category, message, data);
        console.info('ℹ️ [INFO]', logEntry.timestamp, `[${category}]`, message, data || '');
    }

    logWarn(message, data = null, category = 'WARN') {
        if (!this.isEnabled) return;
        
        const logEntry = this.formatMessage('WARN', category, message, data);
        console.warn('⚠️ [WARN]', logEntry.timestamp, `[${category}]`, message, data || '');
    }

    logError(message, error = null, category = 'ERROR') {
        if (!this.isEnabled) return;
        
        const errorData = error ? {
            name: error.name,
            message: error.message,
            stack: error.stack
        } : null;
        
        const logEntry = this.formatMessage('ERROR', category, message, errorData);
        console.error('❌ [ERROR]', logEntry.timestamp, `[${category}]`, message, errorData || '');
    }

    // User Interaction Logging
    logUserInput(inputType, value, element = null) {
        this.logInfo(`User input detected: ${inputType}`, {
            inputType,
            value: typeof value === 'string' ? value.substring(0, 100) + (value.length > 100 ? '...' : '') : value,
            elementId: element?.id,
            elementClass: element?.className
        }, 'USER_INPUT');
    }

    logUserClick(elementInfo, coordinates = null) {
        this.logInfo('User click detected', {
            element: elementInfo,
            coordinates,
            timestamp: new Date().toISOString()
        }, 'USER_CLICK');
    }

    logUserSelection(selectionType, selectedValue, options = null) {
        this.logInfo(`User selection: ${selectionType}`, {
            selectionType,
            selectedValue,
            availableOptions: options
        }, 'USER_SELECTION');
    }

    // API Request/Response Logging
    logApiRequest(method, url, requestData = null, headers = null) {
        this.logInfo(`API Request: ${method} ${url}`, {
            method,
            url,
            requestData: requestData ? JSON.stringify(requestData).substring(0, 500) + '...' : null,
            headers,
            timestamp: new Date().toISOString()
        }, 'API_REQUEST');
    }

    logApiResponse(method, url, status, responseData = null, duration = null) {
        const isSuccess = status >= 200 && status < 300;
        const logMethod = isSuccess ? this.logInfo.bind(this) : this.logError.bind(this);
        
        logMethod(`API Response: ${method} ${url} - ${status}`, {
            method,
            url,
            status,
            success: isSuccess,
            responseSize: responseData ? JSON.stringify(responseData).length : 0,
            duration: duration ? `${duration}ms` : null,
            timestamp: new Date().toISOString()
        }, 'API_RESPONSE');
    }

    logApiError(method, url, error, requestData = null) {
        this.logError(`API Error: ${method} ${url}`, {
            method,
            url,
            error: error.message || error,
            requestData: requestData ? JSON.stringify(requestData).substring(0, 200) + '...' : null,
            timestamp: new Date().toISOString()
        }, 'API_ERROR');
    }

    // Application State Logging
    logStateChange(component, oldState, newState) {
        this.logDebug(`State change in ${component}`, {
            component,
            oldState,
            newState,
            timestamp: new Date().toISOString()
        }, 'STATE_CHANGE');
    }

    logComponentLifecycle(component, lifecycle, data = null) {
        this.logDebug(`Component lifecycle: ${component} - ${lifecycle}`, {
            component,
            lifecycle,
            data,
            timestamp: new Date().toISOString()
        }, 'COMPONENT');
    }

    // Form Validation Logging
    logValidation(formName, fieldName, isValid, errorMessage = null) {
        const logMethod = isValid ? this.logDebug.bind(this) : this.logWarn.bind(this);
        
        logMethod(`Form validation: ${formName}.${fieldName}`, {
            formName,
            fieldName,
            isValid,
            errorMessage,
            timestamp: new Date().toISOString()
        }, 'VALIDATION');
    }

    // Performance Logging
    logPerformance(operation, duration, details = null) {
        const logMethod = duration > 1000 ? this.logWarn.bind(this) : this.logDebug.bind(this);
        
        logMethod(`Performance: ${operation} took ${duration}ms`, {
            operation,
            duration,
            details,
            timestamp: new Date().toISOString()
        }, 'PERFORMANCE');
    }

    // Comic Generation Specific Logging
    logComicGeneration(step, status, data = null) {
        this.logInfo(`Comic generation: ${step} - ${status}`, {
            step,
            status,
            data,
            timestamp: new Date().toISOString()
        }, 'COMIC_GENERATION');
    }

    logPromptGeneration(mathConcept, options, result = null) {
        this.logInfo('Prompt generation started', {
            mathConcept,
            options,
            result,
            timestamp: new Date().toISOString()
        }, 'PROMPT_GENERATION');
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