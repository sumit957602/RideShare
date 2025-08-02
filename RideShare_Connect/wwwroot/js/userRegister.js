// Handles multi-step user registration with basic email and phone verification.

document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('registerForm');
    const steps = Array.from(document.querySelectorAll('.register-step'));
    const nextBtn = document.getElementById('nextStep');
    const backBtn = document.getElementById('backStep');
    const submitBtn = document.getElementById('submitRegister');
    const emailInput = document.getElementById('email');
    const phoneInput = document.getElementById('phone');
    const emailCodeInput = document.getElementById('emailCode');
    const phoneCodeInput = document.getElementById('phoneCode');

    let emailCode = '';
    let phoneCode = '';

    function showStep(index) {
        steps.forEach((step, i) => {
            step.classList.toggle('active', i === index);
        });
    }

    function generateCode() {
        return Math.floor(100000 + Math.random() * 900000).toString();
    }

    function sendCodes() {
        emailCode = generateCode();
        phoneCode = generateCode();
        // In a real app, codes would be sent via API. Displaying for demo only.
        const emailDisplay = document.getElementById('emailCodeDisplay');
        const phoneDisplay = document.getElementById('phoneCodeDisplay');
        emailDisplay.textContent = emailCode;
        phoneDisplay.textContent = phoneCode;
    }

    nextBtn?.addEventListener('click', () => {
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }
        sendCodes();
        showStep(1);
    });

    backBtn?.addEventListener('click', () => {
        showStep(0);
    });

    function verifyCodes() {
        const emailValid = emailCodeInput.value === emailCode;
        const phoneValid = phoneCodeInput.value === phoneCode;
        if (emailValid && phoneValid) {
            submitBtn.disabled = false;
        } else {
            submitBtn.disabled = true;
        }
    }

    emailCodeInput?.addEventListener('input', verifyCodes);
    phoneCodeInput?.addEventListener('input', verifyCodes);
});

