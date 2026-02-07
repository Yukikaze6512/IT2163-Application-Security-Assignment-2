// ===== Password Strength =====
function initPwStrength(inputId, barId, textId) {
    const input = document.getElementById(inputId);
    const bar = document.getElementById(barId);
    const text = document.getElementById(textId);
    if (!input || !bar) return;

    const reqs = {
        length: document.getElementById('req-length'),
        lower: document.getElementById('req-lower'),
        upper: document.getElementById('req-upper'),
        digit: document.getElementById('req-digit'),
        special: document.getElementById('req-special')
    };

    input.addEventListener('input', function () {
        const pw = this.value;
        let s = 0;
        const checks = {
            length: pw.length >= 12,
            lower: /[a-z]/.test(pw),
            upper: /[A-Z]/.test(pw),
            digit: /\d/.test(pw),
            special: /[\W_]/.test(pw)
        };
        for (const k in checks) {
            if (checks[k]) s++;
            if (reqs[k]) reqs[k].classList.toggle('met', checks[k]);
        }
        const pct = (s / 5) * 100;
        bar.style.width = pct + '%';
        const colors = ['#ef4444', '#ef4444', '#f97316', '#eab308', '#16a34a'];
        const labels = ['Very weak', 'Weak', 'Fair', 'Good', 'Strong'];
        const i = Math.max(0, s - 1);
        bar.style.backgroundColor = colors[i];
        if (text) { text.textContent = pw.length ? labels[i] : ''; text.style.color = colors[i]; }
    });
}

// ===== OTP =====
function initOtp(sel, hiddenName, opts) {
    opts = Object.assign({ type: 'number', placeholder: '', autoSubmit: false, onComplete: null }, opts || {});
    const ct = document.querySelector(sel);
    if (!ct) return;
    const ins = ct.querySelectorAll('.otp-c');
    const hid = document.querySelector('input[name="' + hiddenName + '"]');

    if (opts.placeholder) ins.forEach(function (el, i) { el.placeholder = opts.placeholder.length === 1 ? opts.placeholder : (opts.placeholder[i] || ''); });
    ins.forEach(function (el, i) {
        el.maxLength = 1; el.autocomplete = 'one-time-code';
        el.setAttribute('aria-label', 'Digit ' + (i + 1) + ' of ' + ins.length);
        if (opts.type === 'number') { el.inputMode = 'numeric'; el.pattern = '[0-9]*'; }
    });

    function collect() { let v = ''; ins.forEach(function (el) { v += el.value; }); if (hid) hid.value = v; return v; }
    function mark(el) { el.classList.toggle('filled', !!el.value); if (el.value) { el.classList.add('pop'); setTimeout(function () { el.classList.remove('pop'); }, 120); } }

    ins.forEach(function (el, i) {
        el.addEventListener('input', function () {
            let v = this.value; if (opts.type === 'number') v = v.replace(/\D/g, '');
            this.value = v.charAt(0) || ''; mark(this);
            const full = collect();
            if (this.value && i < ins.length - 1) { ins[i + 1].focus(); ins[i + 1].select(); }
            if (full.length === ins.length) { if (opts.onComplete) opts.onComplete(full); if (opts.autoSubmit) { const f = ct.closest('form'); if (f) f.submit(); } }
        });
        el.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace') { if (!this.value && i > 0) { ins[i - 1].focus(); ins[i - 1].value = ''; mark(ins[i - 1]); collect(); e.preventDefault(); } else { this.value = ''; mark(this); collect(); } }
            else if (e.key === 'ArrowLeft' && i > 0) { e.preventDefault(); ins[i - 1].focus(); ins[i - 1].select(); }
            else if (e.key === 'ArrowRight' && i < ins.length - 1) { e.preventDefault(); ins[i + 1].focus(); ins[i + 1].select(); }
        });
        el.addEventListener('focus', function () { this.select(); });
        el.addEventListener('paste', function (e) {
            e.preventDefault();
            let p = (e.clipboardData || window.clipboardData).getData('text').trim();
            if (opts.type === 'number') p = p.replace(/\D/g, '');
            for (let j = 0; j < ins.length; j++) { ins[j].value = p.charAt(j) || ''; mark(ins[j]); }
            collect();
            const fi = Math.min(p.length, ins.length - 1);
            ins[fi].focus();
            if (p.length >= ins.length) { if (opts.onComplete) opts.onComplete(collect()); if (opts.autoSubmit) { const f = ct.closest('form'); if (f) f.submit(); } }
        });
    });
    if (document.activeElement === document.body) ins[0].focus();
}

// ===== Helpers =====
document.addEventListener('DOMContentLoaded', function () {
    // Lucide icons
    if (typeof lucide !== 'undefined') lucide.createIcons();

    // NRIC uppercase
    const nric = document.querySelector('[name="Input.NRIC"]');
    if (nric) nric.addEventListener('input', function () { this.value = this.value.toUpperCase(); });

    // Email blur check
    const email = document.querySelector('[name="Input.Email"]');
    if (email) email.addEventListener('blur', function () {
        this.classList.toggle('is-invalid', !!(this.value && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.value)));
    });

    // Resume file check
    const resume = document.querySelector('[name="Input.Resume"]');
    if (resume) resume.addEventListener('change', function () {
        const f = this.files[0]; if (!f) return;
        const ext = f.name.toLowerCase().substring(f.name.lastIndexOf('.'));
        if (!['.docx', '.pdf'].includes(ext)) { alert('Only .docx and .pdf files are allowed.'); this.value = ''; return; }
        if (f.size > 10485760) { alert('File size cannot exceed 10 MB.'); this.value = ''; }
    });

    // Validation errors rendered server-side via _ValidationErrors partial
});