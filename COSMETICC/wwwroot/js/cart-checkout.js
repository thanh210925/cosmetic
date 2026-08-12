/* ================= CART & CHECKOUT JAVASCRIPT ================= */

// Cart Index Page Logic
function toggleSelectAll(masterCheckbox) {
    const checkboxes = document.querySelectorAll('.cart-item-checkbox');
    checkboxes.forEach(cb => {
        cb.checked = masterCheckbox.checked;
    });
    recalculateCart();
}

function adjustQty(cartItemId, delta) {
    const input = document.getElementById("qty-input-" + cartItemId);
    if (!input) return;
    let newQty = parseInt(input.value) + delta;
    if (newQty <= 0) {
        removeItem(cartItemId);
        return;
    }

    fetch(`/Cart/UpdateQuantity?cartItemId=${cartItemId}&quantity=${newQty}`, { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                input.value = newQty;
                recalculateRow(cartItemId, newQty);
            } else {
                alert(data.message || 'Lỗi khi cập nhật số lượng.');
            }
        })
        .catch(err => {
            console.error(err);
            alert('Lỗi kết nối máy chủ.');
        });
}

function recalculateRow(cartItemId, qty) {
    const unitPriceSpan = document.getElementById("unit-price-" + cartItemId);
    if (!unitPriceSpan) return;
    const unitPrice = parseFloat(unitPriceSpan.getAttribute("data-price"));
    const subtotalSpan = document.getElementById("subtotal-" + cartItemId);
    
    const newSubtotal = unitPrice * qty;
    if (subtotalSpan) subtotalSpan.innerText = new Intl.NumberFormat('vi-VN').format(newSubtotal) + " đ";
    
    recalculateCart();
}

function recalculateCart() {
    let total = 0;
    let selectedIds = [];
    let checkedItemsCount = 0;

    const allCheckboxes = document.querySelectorAll('.cart-item-checkbox');
    const totalItems = allCheckboxes.length;

    allCheckboxes.forEach(cb => {
        if (cb.checked) {
            checkedItemsCount++;
            const id = cb.getAttribute('data-id');
            selectedIds.push(id);

            const unitPriceSpan = document.getElementById("unit-price-" + id);
            const unitPrice = parseFloat(unitPriceSpan.getAttribute("data-price"));
            const qtyInput = document.getElementById("qty-input-" + id);
            const qty = qtyInput ? (parseInt(qtyInput.value) || 1) : 1;
            total += unitPrice * qty;
        }
    });

    const selectAllCheck = document.getElementById("selectAllCheck");
    if (selectAllCheck) {
        selectAllCheck.checked = (totalItems > 0 && checkedItemsCount === totalItems);
    }

    const countSpan = document.getElementById("selectedCountText");
    if (countSpan) countSpan.innerText = checkedItemsCount;

    const subtotalSpan = document.getElementById("summarySubtotal");
    const totalSpan = document.getElementById("summaryTotal");
    if (subtotalSpan) subtotalSpan.innerText = new Intl.NumberFormat('vi-VN').format(total) + " đ";
    if (totalSpan) totalSpan.innerText = new Intl.NumberFormat('vi-VN').format(total) + " đ";
    
    const checkoutBtn = document.getElementById("checkoutBtn");
    if (checkoutBtn) {
        if (totalItems === 0) {
            checkoutBtn.classList.add("disabled");
            checkoutBtn.style.background = "#cbd5e1";
            checkoutBtn.style.cursor = "not-allowed";
            checkoutBtn.style.pointerEvents = "none";
            checkoutBtn.innerHTML = '<i class="bi bi-cart-x me-2"></i> GIỎ HÀNG TRỐNG';
            checkoutBtn.removeAttribute("href");
        } else if (checkedItemsCount === 0) {
            checkoutBtn.classList.add("disabled");
            checkoutBtn.style.background = "#cbd5e1";
            checkoutBtn.style.cursor = "not-allowed";
            checkoutBtn.style.pointerEvents = "none";
            checkoutBtn.innerHTML = '<i class="bi bi-exclamation-circle me-2"></i> CHỌN SẢN PHẨM ĐỂ THANH TOÁN';
            checkoutBtn.removeAttribute("href");
        } else {
            checkoutBtn.classList.remove("disabled");
            checkoutBtn.style.background = "linear-gradient(135deg, #9b0022, #c70d3a)";
            checkoutBtn.style.cursor = "pointer";
            checkoutBtn.style.pointerEvents = "auto";
            checkoutBtn.innerHTML = `<i class="bi bi-bag-check-fill me-2"></i> TIẾN HÀNH ĐẶT HÀNG (${checkedItemsCount})`;
            checkoutBtn.setAttribute("href", `/Cart/Checkout?selectedIds=${selectedIds.join(',')}`);
        }
    }
}

function removeItem(cartItemId) {
    if (!confirm("Bạn có chắc chắn muốn xóa sản phẩm này khỏi giỏ hàng?")) return;

    fetch(`/Cart/RemoveItem?cartItemId=${cartItemId}`, { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                const row = document.getElementById("cart-item-row-" + cartItemId);
                if (row) {
                    row.style.transform = "scale(0.9)";
                    row.style.opacity = "0";
                    row.style.transition = "all 0.3s ease";
                    setTimeout(() => {
                        const brandGroup = row.closest('.brand-group-wrapper');
                        row.remove();
                        if (brandGroup && brandGroup.querySelectorAll('.cart-item-row').length === 0) {
                            brandGroup.remove();
                        }
                        recalculateCart();
                    }, 300);
                }
            } else {
                alert(data.message || 'Lỗi khi xóa sản phẩm.');
            }
        })
        .catch(err => {
            console.error(err);
            alert('Lỗi kết nối máy chủ.');
        });
}

// Checkout Page Logic
function selectPaymentMethod(method) {
    const payCOD = document.getElementById('payCOD');
    const payVNPay = document.getElementById('payVNPay');
    if (payCOD) payCOD.checked = (method === 'COD');
    if (payVNPay) payVNPay.checked = (method === 'VNPay');
    
    const cardCOD = document.getElementById('cardCOD');
    const cardVNPay = document.getElementById('cardVNPay');
    if (cardCOD) cardCOD.classList.toggle('selected', method === 'COD');
    if (cardVNPay) cardVNPay.classList.toggle('selected', method === 'VNPay');
}

function closeModalById(modalId) {
    const modalEl = document.getElementById(modalId);
    if (!modalEl) return;

    try {
        if (window.bootstrap && bootstrap.Modal) {
            const modal = bootstrap.Modal.getInstance(modalEl) || bootstrap.Modal.getOrCreateInstance(modalEl);
            if (modal) modal.hide();
        }
    } catch (e) {
        console.warn('Bootstrap modal hide error:', e);
    }

    // Forceful Failsafe Cleanup: remove all backdrops & unfreeze body
    setTimeout(() => {
        document.querySelectorAll('.modal-backdrop').forEach(bd => bd.remove());
        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
        if (modalEl) {
            modalEl.classList.remove('show');
            modalEl.style.display = 'none';
            modalEl.removeAttribute('aria-modal');
            modalEl.setAttribute('aria-hidden', 'true');
        }
    }, 100);
}

/* ================= GHN API INTEGRATION ================= */
document.addEventListener("DOMContentLoaded", function () {
    const provinceSelect = document.getElementById("ghnProvinceSelect");
    if (provinceSelect) {
        loadGhnProvinces();
    }
});

function loadGhnProvinces() {
    const provinceSelect = document.getElementById("ghnProvinceSelect");
    if (!provinceSelect) return;

    fetch("/Shipping/GetProvinces")
        .then(res => res.json())
        .then(res => {
            if (res.success && res.data) {
                provinceSelect.innerHTML = '<option value="">-- Chọn Tỉnh / Thành --</option>';
                res.data.forEach(p => {
                    const opt = document.createElement("option");
                    opt.value = p.provinceID;
                    opt.textContent = p.provinceName;
                    provinceSelect.appendChild(opt);
                });
            }
        })
        .catch(err => console.error("Error loading GHN provinces:", err));
}

function onGhnProvinceChange() {
    const provinceSelect = document.getElementById("ghnProvinceSelect");
    const districtSelect = document.getElementById("ghnDistrictSelect");
    const wardSelect = document.getElementById("ghnWardSelect");

    if (!provinceSelect || !districtSelect || !wardSelect) return;

    const provinceId = provinceSelect.value;
    districtSelect.innerHTML = '<option value="">-- Chọn Quận / Huyện --</option>';
    wardSelect.innerHTML = '<option value="">-- Chọn Phường / Xã --</option>';
    districtSelect.disabled = !provinceId;
    wardSelect.disabled = true;

    if (!provinceId) return;

    fetch(`/Shipping/GetDistricts?provinceId=${provinceId}`)
        .then(res => res.json())
        .then(res => {
            if (res.success && res.data) {
                res.data.forEach(d => {
                    const opt = document.createElement("option");
                    opt.value = d.districtID;
                    opt.textContent = d.districtName;
                    districtSelect.appendChild(opt);
                });
            }
        })
        .catch(err => console.error("Error loading GHN districts:", err));
}

function onGhnDistrictChange() {
    const districtSelect = document.getElementById("ghnDistrictSelect");
    const wardSelect = document.getElementById("ghnWardSelect");
    if (!districtSelect || !wardSelect) return;

    const districtId = districtSelect.value;
    wardSelect.innerHTML = '<option value="">-- Chọn Phường / Xã --</option>';
    wardSelect.disabled = !districtId;

    if (!districtId) return;

    fetch(`/Shipping/GetWards?districtId=${districtId}`)
        .then(res => res.json())
        .then(res => {
            if (res.success && res.data) {
                res.data.forEach(w => {
                    const opt = document.createElement("option");
                    opt.value = w.wardCode;
                    opt.textContent = w.wardName;
                    wardSelect.appendChild(opt);
                });
            }
        })
        .catch(err => console.error("Error loading GHN wards:", err));
}

function onGhnWardChange() {
    const districtSelect = document.getElementById("ghnDistrictSelect");
    const wardSelect = document.getElementById("ghnWardSelect");
    const provinceSelect = document.getElementById("ghnProvinceSelect");

    if (!districtSelect || !wardSelect || !provinceSelect) return;

    const districtId = parseInt(districtSelect.value);
    const wardCode = wardSelect.value;
    const provinceName = provinceSelect.options[provinceSelect.selectedIndex]?.text || '';
    const districtName = districtSelect.options[districtSelect.selectedIndex]?.text || '';
    const wardName = wardSelect.options[wardSelect.selectedIndex]?.text || '';

    if (!districtId || !wardCode) return;

    // Update hidden inputs for Order submit
    const hiddenDist = document.getElementById("inputGhnDistrictId");
    const hiddenWard = document.getElementById("inputGhnWardCode");
    const hiddenCity = document.getElementById("inputCity");
    if (hiddenDist) hiddenDist.value = districtId;
    if (hiddenWard) hiddenWard.value = wardCode;
    if (hiddenCity) hiddenCity.value = `${wardName}, ${districtName}, ${provinceName}`;

    // Update full address display
    const displayAddr = document.getElementById("displayFullAddress");
    const specAddr = document.getElementById("inputSpecificAddress")?.value || "";
    if (displayAddr) {
        displayAddr.textContent = `${specAddr ? specAddr + ', ' : ''}${wardName}, ${districtName}, ${provinceName}`;
    }

    // Call GHN API Fee calculation
    const subtotal = typeof subtotalVal !== 'undefined' ? subtotalVal : 0;
    fetch('/Shipping/CalculateFee', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `districtId=${districtId}&wardCode=${encodeURIComponent(wardCode)}&subtotal=${subtotal}`
    })
    .then(res => res.json())
    .then(res => {
        const noticeText = document.getElementById("ghnNoticeText");
        const feeSpan = document.getElementById("displayShippingFee");

        if (res.success) {
            if (typeof shippingFeeVal !== 'undefined') {
                shippingFeeVal = res.fee;
            }
            if (feeSpan) {
                if (res.isFreeShipping) {
                    feeSpan.className = 'text-success fw-bold';
                    feeSpan.textContent = 'Miễn phí';
                } else {
                    feeSpan.className = 'text-danger fw-bold';
                    feeSpan.textContent = res.fee.toLocaleString('vi-VN') + '₫';
                }
            }
            if (noticeText) {
                noticeText.innerHTML = `Phí giao hàng GHN: <strong class="text-danger">${res.fee.toLocaleString('vi-VN')}₫</strong> (Dự kiến giao: <strong>${res.expectedDate}</strong>)`;
            }
            if (typeof recalculateTotal === 'function') {
                recalculateTotal();
            }
        } else {
            if (noticeText) noticeText.textContent = res.message || "Không thể tính phí ship GHN.";
        }
    })
    .catch(err => console.error("Error calculating GHN fee:", err));
}
