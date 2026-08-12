/* ================= GLOBAL CART & QUANTITY STEPPER ================= */
function changeQtyAndSync(productId, delta) {
    const input = document.getElementById(`qtyInput-${productId}`);
    if (!input) return;

    let currentVal = parseInt(input.value) || 1;
    let newVal = currentVal + delta;
    if (newVal < 0) newVal = 0;

    input.value = newVal;

    if (newVal === 0) {
        // If reduced to 0, hide stepper and show Add button
        const btnInit = document.getElementById(`btnInit-${productId}`);
        const qtyRow = document.getElementById(`qtyRow-${productId}`);
        if (btnInit) btnInit.classList.remove('d-none');
        if (qtyRow) qtyRow.classList.add('d-none');

        fetch(`/Cart/RemoveFromCart?productId=${productId}`, { method: 'POST' })
            .then(res => res.json())
            .then(data => {
                if (typeof refreshCartBadge === 'function') refreshCartBadge();
                if (typeof showCartToast === 'function') showCartToast('Đã xóa sản phẩm khỏi giỏ hàng.');
            });
    } else {
        fetch(`/Cart/UpdateQuantity?productId=${productId}&quantity=${newVal}`, { method: 'POST' })
            .then(res => res.json())
            .then(data => {
                if (typeof refreshCartBadge === 'function') refreshCartBadge();
                if (typeof showCartToast === 'function') showCartToast(`Đã cập nhật số lượng: ${newVal}`);
            });
    }
}
