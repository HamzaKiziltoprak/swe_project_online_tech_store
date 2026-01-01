import toast from 'react-hot-toast';

/**
 * Toast Utility Functions
 * Modern, non-blocking notifications for better UX
 */

export const showSuccess = (message: string) => {
    toast.success(message, {
        duration: 3000,
        position: 'bottom-right',
        style: {
            background: '#10B981',
            color: '#fff',
            padding: '16px',
            borderRadius: '8px',
            fontWeight: '500',
        },
        iconTheme: {
            primary: '#fff',
            secondary: '#10B981',
        },
    });
};

export const showError = (message: string) => {
    toast.error(message, {
        duration: 4000,
        position: 'bottom-right',
        style: {
            background: '#EF4444',
            color: '#fff',
            padding: '16px',
            borderRadius: '8px',
            fontWeight: '500',
        },
        iconTheme: {
            primary: '#fff',
            secondary: '#EF4444',
        },
    });
};

export const showInfo = (message: string) => {
    toast(message, {
        duration: 3000,
        position: 'bottom-right',
        icon: 'ℹ️',
        style: {
            background: '#3B82F6',
            color: '#fff',
            padding: '16px',
            borderRadius: '8px',
            fontWeight: '500',
        },
    });
};

export const showLoading = (message: string) => {
    return toast.loading(message, {
        position: 'bottom-right',
        style: {
            background: '#6B7280',
            color: '#fff',
            padding: '16px',
            borderRadius: '8px',
            fontWeight: '500',
        },
    });
};
