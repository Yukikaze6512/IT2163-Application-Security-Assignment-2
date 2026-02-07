// ===== Password Strength Indicator =====
function initPasswordStrengthIndicator(inputId, barId, textId) {
    var input = document.getElementById(inputId);
    var bar = document.getElementById(barId);
    var text = document.getElementById(textId);

    if (!input || !bar) return;

    // Find requirement indicators if they exist
    var reqLength = document.getElementById('req-length');
    var reqLower = document.getElementById('req-lower');
    var reqUpper = document.getElementById('req-upper');
    var reqDigit = document.getElementById('req-digit');
    var reqSpecial = document.getElementById('req-special');

    input.addEventListener('input', function () {
        var pw = this.value;
        var strength = 0;
        var hasLength = pw.length >= 12;
        var hasLower = /[a-z]/.test(pw);
        var hasUpper = /[A-Z]/.test(pw);
        var hasDigit = /\d/.test(pw);
        var hasSpecial = /[\W_]/.test(pw);

        if (hasLength) strength++;
        if (hasLower) strength++;
        if (hasUpper) strength++;
        if (hasDigit) strength++;
        if (hasSpecial) strength++;

        // Update requirement badges
        toggleReq(reqLength, hasLength);
        toggleReq(reqLower, hasLower);
        toggleReq(reqUpper, hasUpper);
        toggleReq(reqDigit, hasDigit);
        toggleReq(reqSpecial, hasSpecial);

        // Update bar
        var pct = (strength / 5) * 100;
        bar.style.width = pct + '%';

        var colors = ['#dc3545', '#dc3545', '#fd7e14', '#ffc107', '#198754'];
        var labels = ['Very Weak', 'Weak', 'Fair', 'Good', 'Strong'];
        var idx = Math.max(0, strength - 1);
        bar.style.backgroundColor = colors[idx];

        if (text) {
            text.textContent = pw.length > 0 ? labels[idx] : '';
            text.style.color = colors[idx];
        }
    });

    function toggleReq(el, met) {
        if (!el) return;
        if (met) {
            el.classList.add('met');
            var icon = el.querySelector('i');
            if (icon) { icon.className = 'bi bi-check-circle-fill'; }
        } else {
            el.classList.remove('met');
            var icon = el.querySelector('i');
            if (icon) { icon.className = 'bi bi-circle'; }
        }
    }
}

// ===== OTP Input Component =====
function initOtpInput(containerSelector, hiddenInputName, options) {
    var opts = Object.assign({
        length: 6,
        type: 'number',       // 'number' or 'text'
        autoSubmit: false,
        placeholder: '',
        onComplete: null
    }, options || {});

    var container = document.querySelector(containerSelector);
    if (!container) return;

    var inputs = container.querySelectorAll('.form-otp-control');
    var hiddenInput = document.querySelector('input[name="' + hiddenInputName + '"]');

    // Set placeholders
    if (opts.placeholder) {
        inputs.forEach(function (inp, i) {
            inp.placeholder = opts.placeholder.length === 1
                ? opts.placeholder
                : (opts.placeholder[i] || '');
        });
    }

    // Set input attributes
    inputs.forEach(function (inp, i) {
        inp.setAttribute('maxlength', '1');
        inp.setAttribute('autocomplete', 'one-time-code');
        inp.setAttribute('aria-label', 'Digit ' + (i + 1) + ' of ' + inputs.length);
        if (opts.type === 'number') {
            inp.setAttribute('inputmode', 'numeric');
            inp.setAttribute('pattern', '[0-9]*');
        }
    });

    function collectValue() {
        var val = '';
        inputs.forEach(function (inp) { val += inp.value; });
        if (hiddenInput) hiddenInput.value = val;
        return val;
    }

    function setFilled(inp) {
        if (inp.value) {
            inp.classList.add('filled', 'pop');
            setTimeout(function () { inp.classList.remove('pop'); }, 150);
        } else {
            inp.classList.remove('filled');
        }
    }

    inputs.forEach(function (inp, idx) {
        // Input event
        inp.addEventListener('input', function (e) {
            var v = this.value;

            // Filter by type
            if (opts.type === 'number') {
                v = v.replace(/\D/g, '');
            }
            this.value = v.charAt(0) || '';
            setFilled(this);

            var fullValue = collectValue();

            // Move to next
            if (this.value && idx < inputs.length - 1) {
                inputs[idx + 1].focus();
                inputs[idx + 1].select();
            }

            // Check complete
            if (fullValue.length === inputs.length) {
                if (opts.onComplete) opts.onComplete(fullValue);
                if (opts.autoSubmit) {
                    var form = container.closest('form');
                    if (form) form.submit();
                }
            }
        });

        // Keydown for navigation
        inp.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace') {
                if (!this.value && idx > 0) {
                    inputs[idx - 1].focus();
                    inputs[idx - 1].value = '';
                    setFilled(inputs[idx - 1]);
                    collectValue();
                    e.preventDefault();
                } else {
                    this.value = '';
                    setFilled(this);
                    collectValue();
                }
            } else if (e.key === 'ArrowLeft' && idx > 0) {
                e.preventDefault();
                inputs[idx - 1].focus();
                inputs[idx - 1].select();
            } else if (e.key === 'ArrowRight' && idx < inputs.length - 1) {
                e.preventDefault();
                inputs[idx + 1].focus();
                inputs[idx + 1].select();
            } else if (e.key === 'Delete') {
                this.value = '';
                setFilled(this);
                collectValue();
            }
        });

        // Focus — select content
        inp.addEventListener('focus', function () {
            this.select();
        });

        // Paste support
        inp.addEventListener('paste', function (e) {
            e.preventDefault();
            var pasted = (e.clipboardData || window.clipboardData).getData('text').trim();
            if (opts.type === 'number') pasted = pasted.replace(/\D/g, '');

            for (var j = 0; j < inputs.length; j++) {
                inputs[j].value = pasted.charAt(j) || '';
                setFilled(inputs[j]);
            }
            collectValue();

            // Focus last filled or the one after
            var focusIdx = Math.min(pasted.length, inputs.length - 1);
            inputs[focusIdx].focus();

            if (pasted.length >= inputs.length) {
                if (opts.onComplete) opts.onComplete(collectValue());
                if (opts.autoSubmit) {
                    var form = container.closest('form');
                    if (form) form.submit();
                }
            }
        });
    });

    // Focus first on init
    if (document.activeElement === document.body || !container.contains(document.activeElement)) {
        inputs[0].focus();
    }
}

// ===== Client-side helpers =====
document.addEventListener('DOMContentLoaded', function () {
    // Auto-uppercase NRIC input
    var nricInput = document.querySelector('[name="Input.NRIC"]');
    if (nricInput) {
        nricInput.addEventListener('input', function () {
            this.value = this.value.toUpperCase();
        });
    }

    // Client-side email check
    var emailInput = document.querySelector('[name="Input.Email"]');
    if (emailInput) {
        emailInput.addEventListener('blur', function () {
            var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (this.value && !emailRegex.test(this.value)) {
                this.classList.add('is-invalid');
            } else {
                this.classList.remove('is-invalid');
            }
        });
    }

    // Resume file validation
    var resumeInput = document.querySelector('[name="Input.Resume"]');
    if (resumeInput) {
        resumeInput.addEventListener('change', function () {
            var file = this.files[0];
            if (file) {
                var validExt = ['.docx', '.pdf'];
                var ext = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));
                if (!validExt.includes(ext)) {
                    alert('Only .docx and .pdf files are allowed.');
                    this.value = '';
                    return;
                }
                if (file.size > 10 * 1024 * 1024) {
                    alert('File size cannot exceed 10 MB.');
                    this.value = '';
                }
            }
        });
    }

    // Add loading state to submit buttons
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            var btn = form.querySelector('button[type="submit"]');
            if (btn && !btn.dataset.noSpinner) {
                btn.disabled = true;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Please wait...';
            }
        });
    });
});
