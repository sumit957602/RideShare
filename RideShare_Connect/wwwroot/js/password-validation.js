document.addEventListener('DOMContentLoaded', () => {
  const pairs = [
    { password: document.getElementById('password'), confirm: document.getElementById('confirmPassword') },
    { password: document.getElementById('newPassword'), confirm: document.getElementById('confirmNewPassword') }
  ];

  pairs.forEach(({ password, confirm }) => {
    if (password && confirm) {
      const validate = () => {
        if (password.value !== confirm.value) {
          confirm.setCustomValidity('Passwords do not match.');
        } else {
          confirm.setCustomValidity('');
        }
      };
      password.addEventListener('change', validate);
      confirm.addEventListener('keyup', validate);
    }
  });
});
