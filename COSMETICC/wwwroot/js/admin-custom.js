/* ================= ADMIN CUSTOM JAVASCRIPT ================= */

function previewAvatar(input) {
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function(e) {
            const preview = document.getElementById('avatarPreview');
            if (preview) preview.src = e.target.result;
        }
        reader.readAsDataURL(input.files[0]);
    }
}
