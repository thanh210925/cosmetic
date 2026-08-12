/* ================= BLOG & SOCIAL JAVASCRIPT ================= */

function toggleLike(postId, btn) {
    fetch(`/Blog/ToggleLike?postId=${postId}`, { method: 'POST' })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                const countSpan = btn.querySelector('.like-count');
                countSpan.textContent = data.likes;
                if (data.isLiked) {
                    btn.classList.add('liked');
                } else {
                    btn.classList.remove('liked');
                }
            } else {
                alert(data.message);
            }
        })
        .catch(err => console.error(err));
}

function submitComment(postId) {
    const input = document.getElementById(`comment-input-${postId}`);
    const content = input ? input.value.trim() : '';
    if (!content) return;

    const formData = new FormData();
    formData.append('postId', postId);
    formData.append('content', content);

    fetch('/Blog/AddComment', {
        method: 'POST',
        body: formData
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            input.value = '';
            location.reload();
        } else {
            alert(data.message);
        }
    })
    .catch(err => console.error(err));
}
