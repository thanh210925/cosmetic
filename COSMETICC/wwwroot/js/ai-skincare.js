/* ================= AI HUB & ROUTINE JAVASCRIPT ================= */

function switchTime(time, btn) {
    document.querySelectorAll('.time-tab').forEach(b => b.classList.remove('active'));
    if (btn) btn.classList.add('active');

    const morning = document.getElementById('morning-routine');
    const evening = document.getElementById('evening-routine');

    if (time === 'morning') {
        if (morning) morning.style.display = 'block';
        if (evening) evening.style.display = 'none';
    } else {
        if (morning) morning.style.display = 'none';
        if (evening) evening.style.display = 'block';
    }
}

function selectChip(el) {
    document.querySelectorAll('.quick-chip').forEach(c => c.classList.remove('active'));
    if (el) el.classList.add('active');
}
