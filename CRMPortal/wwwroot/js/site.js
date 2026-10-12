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




// =========================================================
// CRM PORTAL APPLICATION SESSION KEEP-ALIVE
//
// This keeps the APPLICATION LOGIN session alive.
//
// IMPORTANT:
// This is NOT Employee Attendance Sign In / Sign Out.
//
// Employee Attendance:
//     Sign In / Sign Out
//
// Application Login:
//     UserId / RoleId / EmployeeId etc.
// =========================================================

(function () {

    // Run every 5 minutes
    const keepAliveInterval = 5 * 60 * 1000;


    async function keepApplicationSessionAlive() {

        try {

            const response = await fetch(
                '/Account/KeepSessionAlive',
                {
                    method: 'GET',
                    credentials: 'same-origin',
                    cache: 'no-store'
                }
            );


            // =====================================================
            // APPLICATION SESSION EXPIRED
            // =====================================================

            if (response.status === 401) {

                console.warn(
                    "CRMPortal application session has expired."
                );

                return;
            }


            // =====================================================
            // SESSION SUCCESSFULLY REFRESHED
            // =====================================================

            if (response.ok) {

                console.log(
                    "CRMPortal application session refreshed."
                );

            }

        }
        catch (error) {

            console.error(
                "CRMPortal session keep-alive failed:",
                error
            );

        }

    }


    // =========================================================
    // START HEARTBEAT
    // =========================================================

    setInterval(
        keepApplicationSessionAlive,
        keepAliveInterval
    );

})();





// =========================================================
// YOUR EXISTING site.js CODE
// =========================================================

// Existing functions...


// Example:
// function startEmployeeTimer(...) {
//     ...
// }


// =========================================================
// OTHER EXISTING site.js CODE
// =========================================================

// Your other JavaScript...


// =========================================================
// CRM PORTAL APPLICATION SESSION KEEP-ALIVE
// =========================================================

(function () {

    // =========================================================
    // ONLY RUN FOR LOGGED-IN USERS
    // =========================================================

    if (!window.crmPortalLoggedIn) {
        return;
    }


    // Run every 5 minutes
    const keepAliveInterval = 5 * 60 * 1000;


    async function keepApplicationSessionAlive() {

        try {

            const response = await fetch(
                '/Account/KeepSessionAlive',
                {
                    method: 'GET',
                    credentials: 'same-origin',
                    cache: 'no-store'
                }
            );


            if (response.status === 401) {

                console.warn(
                    "CRMPortal application session has expired."
                );

                return;
            }


            if (response.ok) {

                console.log(
                    "CRMPortal application session refreshed."
                );

            }

        }
        catch (error) {

            console.error(
                "CRMPortal session keep-alive failed:",
                error
            );

        }

    }


    // =========================================================
    // START SESSION HEARTBEAT
    // =========================================================

    setInterval(
        keepApplicationSessionAlive,
        keepAliveInterval
    );

})();

$(document).ready(function () {

    $('.searchable-dropdown').select2({
        placeholder: "Select Employee",
        allowClear: true,
        width: '100%'
    });

});


$(document).on("click", ".copy-btn", function () {

    var value = $(this).data("copy");

    navigator.clipboard.writeText(value).then(function () {

        // show copied message

    });

});


$(document).on("click", ".employee-details-link", function () {
    debugger;
    var employeeId = $(this).data("id");
    debugger;
    $.ajax({

        url: '/HR/EmployeeDetails',
        type: 'GET',
        data: { id: employeeId },
        
        success: function (result) {

            $("#employeeDetailsContainer").html(result);

            var modal = new bootstrap.Modal(
                document.getElementById("employeeDetailsModal")
            );

            modal.show();
        },

        error: function (xhr, status, error) {
            debugger;
            console.log("Status Code:", xhr.status);
            console.log("Status:", status);
            console.log("Error:", error);
            console.log("Response:", xhr.responseText);

            alert("Unable to load employee details. Status Code: " + xhr.status);
        }
    });

});


document.getElementById('employeeDetailsModal')
    .addEventListener('hidden.bs.modal', function () {

        // Remove any leftover backdrop
        document.querySelectorAll('.modal-backdrop').forEach(function (backdrop) {
            backdrop.remove();
        });

        // Remove Bootstrap's modal-open class
        document.body.classList.remove('modal-open');

        // Reset body styles
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';

    });




function toggleSalary(button) {

    const cardBody = button.closest(".card-body");

    const salaryValue = cardBody.querySelector(".salary-value");

    const icon = button.querySelector("i");

    const isHidden = salaryValue.textContent.trim() === "₹ ••••••";

    if (isHidden) {

        // Show salary
        salaryValue.textContent = salaryValue.dataset.value;

        icon.classList.remove("bi-eye");
        icon.classList.add("bi-eye-slash");

        button.title = "Hide Salary";

    } else {

        // Hide salary
        salaryValue.textContent = "₹ ••••••";

        icon.classList.remove("bi-eye-slash");
        icon.classList.add("bi-eye");

        button.title = "Show Salary";
    }
}


/* Employee Master page helpers.
Your existing site.js still opens the modal and loads the partial view;
nothing here changes that. This file only prevents the "black screen
after closing the modal" problem. */

(function () {
    'use strict';

    var modalEl = document.getElementById('employeeDetailsModal');
    if (!modalEl) return;

    // 1) Make the modal a direct child of <body>.
    //    If a parent has transform / overflow / z-index, the dark backdrop can
    //    end up on top of the modal and the page looks black and unclickable.
    if (modalEl.parentElement !== document.body) {
        document.body.appendChild(modalEl);
    }

    // 2) Safety net: after the modal is fully hidden, remove any leftover
    //    backdrop and unlock the page scroll.
    modalEl.addEventListener('hidden.bs.modal', function () {
        if (document.querySelector('.modal.show')) return;

        document.querySelectorAll('.modal-backdrop').forEach(function (b) {
            b.remove();
        });

        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    });
})();