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

function startEmployeeTimer(signInTime, sessionStatus, signOutTime) {

    const timerElement =
        document.getElementById("workingTimer");

    const signInDisplay =
        document.getElementById("signInDisplay");

    const signOutDisplay =
        document.getElementById("signOutDisplay");

    const sessionStatusDisplay =
        document.getElementById("sessionStatusDisplay");

    const totalWorkingHours =
        document.getElementById("totalWorkingHours");


    // ==========================================
    // CLEAR EXISTING TIMER
    // ==========================================

    if (window.employeeTimerInterval) {

        clearInterval(
            window.employeeTimerInterval
        );

        window.employeeTimerInterval = null;
    }


    // ==========================================
    // CLEAR EXISTING SIGN-OUT RESET TIMER
    // ==========================================

    if (window.employeeSignOutResetInterval) {

        clearInterval(
            window.employeeSignOutResetInterval
        );

        window.employeeSignOutResetInterval = null;
    }


    // ==========================================
    // FUNCTION TO RESET EMPLOYEE UI
    // ==========================================

    function resetEmployeeUI() {

        // Sign In
        if (signInDisplay) {
            signInDisplay.innerText = "--";
        }

        // Sign Out
        if (signOutDisplay) {
            signOutDisplay.innerText = "--";
        }

        // Status
        if (sessionStatusDisplay) {
            sessionStatusDisplay.innerText =
                "Not Signed In";
        }

        // Working Timer
        if (timerElement) {
            timerElement.innerText =
                "00:00:00";
        }

        // Total Working Hours
        if (totalWorkingHours) {
            totalWorkingHours.innerText =
                "--";
        }

        console.log(
            "Employee dashboard UI automatically reset."
        );
    }


    // ==========================================
    // IF EMPLOYEE IS SIGNED OUT
    // ==========================================

    if (sessionStatus === "Signed Out") {

        // Keep timer stopped
        if (timerElement) {
            timerElement.innerText =
                "00:00:00";
        }


        // ------------------------------------------
        // Check Sign Out Time
        // ------------------------------------------

        if (signOutTime) {

            const signOutDate =
                new Date(signOutTime);


            function checkSignOutReset() {

                const now =
                    new Date();

                const difference =
                    now.getTime() -
                    signOutDate.getTime();

                // 1 hour = 60 minutes
                const oneHour =
                    60 * 60 * 1000;


                // --------------------------------------
                // ONE HOUR COMPLETED
                // --------------------------------------

                if (difference >= oneHour) {

                    resetEmployeeUI();

                    clearInterval(
                        window.employeeSignOutResetInterval
                    );

                    window.employeeSignOutResetInterval =
                        null;
                }
            }


            // Check immediately
            checkSignOutReset();


            // Check every 1 minute
            window.employeeSignOutResetInterval =
                setInterval(
                    checkSignOutReset,
                    60 * 1000
                );
        }

        return;
    }


    // ==========================================
    // EMPLOYEE NOT SIGNED IN
    // ==========================================

    if (
        !signInTime ||
        sessionStatus !== "Signed In"
    ) {

        if (timerElement) {
            timerElement.innerText =
                "00:00:00";
        }

        return;
    }


    // ==========================================
    // EMPLOYEE SIGNED IN
    // ==========================================

    const signInDate =
        new Date(signInTime);


    function updateTimer() {

        const now =
            new Date();

        const diff =
            now.getTime() -
            signInDate.getTime();


        if (diff < 0) {
            return;
        }


        const hours =
            Math.floor(
                diff / 3600000
            );

        const minutes =
            Math.floor(
                (diff % 3600000) / 60000
            );

        const seconds =
            Math.floor(
                (diff % 60000) / 1000
            );


        if (timerElement) {

            timerElement.innerText =
                String(hours).padStart(2, "0") +
                ":" +
                String(minutes).padStart(2, "0") +
                ":" +
                String(seconds).padStart(2, "0");
        }
    }


    // Initial timer update
    updateTimer();


    // Start timer
    window.employeeTimerInterval =
        setInterval(
            updateTimer,
            1000
        );
}


document.addEventListener("DOMContentLoaded", function ()
{
    // Automatically hide Success message after 20 seconds var successAlert = document.getElementById("successAlert");
    if (successAlert)
    {
        setTimeout(function ()
        {
            var alert = bootstrap.Alert.getOrCreateInstance(successAlert);
            alert.close();
        }, 10000); // 20 seconds 
    } // Automatically hide Error message after 20 seconds 
        var errorAlert = document.getElementById("errorAlert"); 
        if (errorAlert)
        {
            setTimeout(function ()
            {
                var alert = bootstrap.Alert.getOrCreateInstance(errorAlert);
                alert.close();
            }, 20000); // 20 seconds 
        }
});




document.addEventListener("DOMContentLoaded", function () {

    const employeeName =
        document.getElementById("employeeName");

    const employeeId =
        document.getElementById("EmployeeId");

    const employeeDropdown =
        document.getElementById("employeeDropdown");

    const employeeOptions =
        document.querySelectorAll(".employee-option");


    // ==========================================
    // SHOW DROPDOWN WHEN USER CLICKS
    // ==========================================

    employeeName.addEventListener("focus", function () {

        employeeDropdown.style.display = "block";

        filterEmployees();

    });


    // ==========================================
    // SEARCH EMPLOYEE
    // ==========================================

    employeeName.addEventListener("input", function () {

        employeeId.value = "";

        filterEmployees();

    });


    // ==========================================
    // FILTER EMPLOYEES
    // ==========================================

    function filterEmployees() {

        const searchText =
            employeeName.value
                .toLowerCase()
                .trim();

        let found = false;


        employeeOptions.forEach(function (option) {

            const searchValue =
                option.getAttribute("data-search")
                    .toLowerCase();

            if (searchValue.includes(searchText)) {

                option.style.display = "block";

                found = true;

            }
            else {

                option.style.display = "none";

            }

        });


        if (found) {

            employeeDropdown.style.display = "block";

        }
        else {

            employeeDropdown.style.display = "none";

        }

    }


    // ==========================================
    // SELECT EMPLOYEE
    // ==========================================

    employeeOptions.forEach(function (option) {

        option.addEventListener("click", function () {

            const id =
                this.getAttribute("data-id");

            const code =
                this.querySelector("strong").innerText;

            const fullName =
                this.innerText
                    .replace(code, "")
                    .replace("-", "")
                    .trim();


            // Set Employee ID
            employeeId.value = id;


            // Show Code + Name
            employeeName.value =
                code + " - " + fullName;


            // Hide dropdown
            employeeDropdown.style.display = "none";

        });

    });


    // ==========================================
    // CLOSE DROPDOWN WHEN CLICKING OUTSIDE
    // ==========================================

    document.addEventListener("click", function (event) {

        if (!employeeName.contains(event.target) &&
            !employeeDropdown.contains(event.target)) {

            employeeDropdown.style.display = "none";

        }

    });

});


$(document).ready(function () {
    $('.searchable-dropdown').select2({
        placeholder: "Select",
        allowClear: true,
        width: '100%'
    });
});


// =========================================================
// PROFILE GREETING
// =========================================================

document.addEventListener("DOMContentLoaded", function () {

    const greeting = document.getElementById("profileGreeting");

    // Profile page does not have the greeting element
    if (!greeting) {
        return;
    }

    const hour = new Date().getHours();

    if (hour >= 5 && hour < 12) {
        greeting.innerText = "Good Morning";
    }
    else if (hour >= 12 && hour < 17) {
        greeting.innerText = "Good Afternoon";
    }
    else if (hour >= 17 && hour < 21) {
        greeting.innerText = "Good Evening";
    }
    else {
        greeting.innerText = "Good Night";
    }

});