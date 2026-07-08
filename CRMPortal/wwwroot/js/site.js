document.addEventListener("DOMContentLoaded", function () {

    const fullName = document.getElementById("FullName");
    const email = document.getElementById("Email");
    const password = document.getElementById("Password");
    const role = document.getElementById("RoleId");
    const button = document.getElementById("btnAddUser");

    if (!fullName || !email || !password || !role || !button) return;

    function validateForm() {

        if (
            fullName.value.trim() !== "" &&
            email.value.trim() !== "" &&
            password.value.trim() !== "" &&
            role.value !== ""
        ) {
            button.disabled = false;
        } else {
            button.disabled = true;
        }
    }

    fullName.addEventListener("input", validateForm);
    email.addEventListener("input", validateForm);
    password.addEventListener("input", validateForm);
    role.addEventListener("change", validateForm);
});



/*document.getElementById("globalSearch").addEventListener("keyup", function () {
    let value = this.value.toLowerCase();

    document.querySelectorAll(".user-card").forEach(card => {

        let text = card.innerText.toLowerCase();

        card.style.display = text.includes(value) ? "flex" : "none";
    });
});*/