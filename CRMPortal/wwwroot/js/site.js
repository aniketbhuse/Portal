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


// Employee Login Timer - MARS CRM Portal

function startEmployeeTimer(signInTime, sessionStatus) {

    // Reset timer if not signed in
    if (!signInTime || sessionStatus !== "SignedIn") {
        document.getElementById("workingTimer").innerText = "00:00:00";
        return;
    }

    const signInDate = new Date(signInTime);

    function updateTimer() {

        const now = new Date();
        const diff = now.getTime() - signInDate.getTime();

        if (diff < 0) return;

        const hours = Math.floor(diff / (1000 * 60 * 60));
        const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((diff % (1000 * 60)) / 1000);

        document.getElementById("workingTimer").innerText =
            String(hours).padStart(2, "0") + ":" +
            String(minutes).padStart(2, "0") + ":" +
            String(seconds).padStart(2, "0");
    }

    // Start immediately
    updateTimer();

// Update every second
    setInterval(updateTimer, 1000);
}


/*document.getElementById("globalSearch").addEventListener("keyup", function () {
    let value = this.value.toLowerCase();

    document.querySelectorAll(".user-card").forEach(card => {

        let text = card.innerText.toLowerCase();

        card.style.display = text.includes(value) ? "flex" : "none";
    });
});*/