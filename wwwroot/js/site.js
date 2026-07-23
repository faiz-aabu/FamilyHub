document.addEventListener('DOMContentLoaded', () => {
    const toastContainer = document.getElementById('toastContainer');
    if (toastContainer) {
        const showToast = (message, type = 'info') => {
            const toastElement = document.createElement('div');
            toastElement.className = `toast align-items-center text-bg-${type} border-0 show`;
            toastElement.role = 'alert';
            toastElement.innerHTML = `<div class="d-flex"><div class="toast-body">${message}</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>`;
            toastContainer.appendChild(toastElement);
            window.setTimeout(() => {
                toastElement.classList.remove('show');
                toastElement.remove();
            }, 3500);
        };

        const successMessage = document.body.getAttribute('data-success-message');
        const errorMessage = document.body.getAttribute('data-error-message');
        if (successMessage) {
            showToast(successMessage, 'success');
        }
        if (errorMessage) {
            showToast(errorMessage, 'danger');
        }
    }

    const initPasswordToggle = () => {
        document.querySelectorAll('.password-toggle-field').forEach(input => {
            const group = input.closest('.input-group');
            if (!group || group.querySelector('.password-toggle-button')) {
                return;
            }

            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'btn btn-outline-secondary password-toggle-button';
            button.setAttribute('aria-label', 'Show password');
            button.innerHTML = '<i class="bi bi-eye"></i>';

            button.addEventListener('click', () => {
                const isHidden = input.type === 'password';
                input.type = isHidden ? 'text' : 'password';
                button.innerHTML = `<i class="bi ${isHidden ? 'bi-eye-slash' : 'bi-eye'}"></i>`;
                button.setAttribute('aria-label', isHidden ? 'Hide password' : 'Show password');
            });

            group.appendChild(button);
        });
    };

    initPasswordToggle();
});
