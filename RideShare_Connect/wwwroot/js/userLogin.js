// Handles two-step login with simple email and phone verification.

document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('loginForm');
    const steps = Array.from(document.querySelectorAll('.login-step'));
    const nextBtn = document.getElementById('loginNext');
    const backBtn = document.getElementById('loginBack');
    const submitBtn = document.getElementById('submitLogin');
    const emailCodeInput = document.getElementById('loginEmailCode');
    const phoneCodeInput = document.getElementById('loginPhoneCode');

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
        const emailDisplay = document.getElementById('loginEmailCodeDisplay');
        const phoneDisplay = document.getElementById('loginPhoneCodeDisplay');
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

    backBtn?.addEventListener('click', () => showStep(0));

    function verifyCodes() {
        const emailValid = emailCodeInput.value === emailCode;
        const phoneValid = phoneCodeInput.value === phoneCode;
        submitBtn.disabled = !(emailValid && phoneValid);
    }

    emailCodeInput?.addEventListener('input', verifyCodes);
    phoneCodeInput?.addEventListener('input', verifyCodes);
});

