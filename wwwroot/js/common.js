/**
 * Common utilities used throughout the application
 * This file contains global utility functions
 */

// Check if running in development mode
const isDevelopment = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';

/**
 * Show a toast notification
 * @param {string} message - The message to display
 * @param {string} type - Type of notification: 'success', 'error', 'info', 'warning'
 * @param {number} [duration=3000] - Duration in milliseconds
 */
function showToast(message, type = 'info', duration = 3000) {
    // Implementation will be added later
    console.log(`[${type.toUpperCase()}] ${message}`);
}

/**
 * Format date to dd/MM/yyyy
 * @param {Date|string} date - The date to format
 * @returns {string} Formatted date string
 */
function formatDate(date) {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleDateString('vi-VN');
}

/**
 * Format number with thousand separators
 * @param {number|string} num - The number to format
 * @returns {string} Formatted number string
 */
function formatNumber(num) {
    if (num === null || num === undefined) return '0';
    return Number(num).toLocaleString('vi-VN');
}

// Make functions available globally
window.appCommon = {
    showToast,
    formatDate,
    formatNumber,
    isDevelopment
};

// Log environment info
if (isDevelopment) {
    console.log('Common utilities initialized in development mode');
}
