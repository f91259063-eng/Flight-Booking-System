
document.addEventListener("DOMContentLoaded", function () {

    const form = document.getElementById("passengerForm");

    if (!form) {
        console.error("Passenger form not found.");
        return;
    }

    const searchUrl = form.dataset.searchUrl;

    if (!searchUrl) {
        console.error("Passenger search URL not found.");
        return;
    }

    // =========================================================
    // Show Message
    // =========================================================

    function showMessage(card, message, type = "info") {

        let messageBox = card.querySelector(".passenger-message");

        if (!messageBox) {
            messageBox = document.createElement("div");
            messageBox.className = "passenger-message";
            card.querySelector(".card-body")?.prepend(messageBox);
        }

        if (!messageBox) return;

        // Correct Bootstrap alert classes
        messageBox.className =
            `alert alert - ${ type } passenger - message`;

        messageBox.textContent = message;
    }

    // =========================================================
    // Get Value From JSON
    // =========================================================

    function getValue(object, ...keys) {

        for (const key of keys) {
            if (
                Object.prototype.hasOwnProperty.call(object, key) &&
                object[key] !== null &&
                object[key] !== undefined
            ) {
                return object[key];
            }
        }

        return "";
    }

    // =========================================================
    // Set Input Value
    // =========================================================

    function setInputValue(card, selector, value) {

        const input = card.querySelector(selector);

        if (!input) {
            console.warn("Input not found:", selector);
            return;
        }

        input.value = value ?? "";

        input.dispatchEvent(new Event("input", { bubbles: true }));
        input.dispatchEvent(new Event("change", { bubbles: true }));
    }

    // =========================================================
    // Search Passenger
    // =========================================================

    async function searchPassenger(card) {

        const searchPassportInput =
            card.querySelector(".search-passport");

        const searchNationalIdInput =
            card.querySelector(".search-national-id");

        const passportNumber =
            searchPassportInput?.value.trim() ?? "";

        const nationalID =
            searchNationalIdInput?.value.trim() ?? "";

        if (!passportNumber && !nationalID) {
            showMessage(
                card,
                "Enter a passport number or national ID.",
                "warning"
            );
            return;
        }

        const url = new URL(searchUrl, window.location.origin);

        // Always send both parameters.
        // This avoids accidentally reusing a missing search parameter.
        url.searchParams.set("passportNumber", passportNumber);
        url.searchParams.set("nationalID", nationalID);

        const searchButton =
            card.querySelector(".search-passenger-btn");

        if (searchButton) {
            searchButton.disabled = true;
            searchButton.textContent = "Searching...";
        }

        try {
            showMessage(card, "Searching passenger...", "info");

            const response = await fetch(url.toString(), {
                method: "GET",
                headers: {
                    "Accept": "application/json"
                },
                credentials: "same-origin"
            });

            const data = await response.json();

            console.log("Passenger search response:", data);

            if (!response.ok || !data.success || !data.passenger) {
                showMessage(
                    card,
                    data.message || "Passenger not found.",
                    "warning"
                );
                return;
            }

            const passenger = data.passenger;

            // Passenger ID
            setInputValue(
                card,
                ".selected-passenger-id",
                getValue(passenger, "id", "Id")
            );

            // Passenger details
            setInputValue(
                card,
                ".passenger-first-name",
                getValue(passenger, "firstName", "FirstName")
            );

            setInputValue(
                card,
                ".passenger-middle-name",
                getValue(passenger, "middleName", "MiddleName")
            );

            setInputValue(
                card,
                ".passenger-last-name",
                getValue(passenger, "lastName", "LastName")
            );

            setInputValue(
                card,
                ".passenger-passport",
                getValue(passenger, "passportNumber", "PassportNumber")
            );

            setInputValue(
                card,
                ".passenger-national-id",
                getValue(passenger, "nationalID", "NationalID")
            );

            setInputValue(
                card,
                ".passenger-phone",
                getValue(passenger, "phone", "Phone")
            );

            setInputValue(
                card,
                ".passenger-email",
                getValue(passenger, "email", "Email")
            );

            // Format date for <input type="date">
            const rawDate = getValue(
                passenger,
                "dateOfBirth",
                "DateOfBirth"
            );

            let formattedDate = "";

            if (rawDate) {
                const dateString = String(rawDate);

                // Handles ISO strings such as 2000-05-20T00:00:00
                if (/^\d{4}-\d{2}-\d{2}/.test(dateString)) {
                    formattedDate = dateString.substring(0, 10);
                } else {
                    const parsedDate = new Date(rawDate);

                    if (!Number.isNaN(parsedDate.getTime())) {
                        const year = parsedDate.getFullYear();
                        const month = String(
                            parsedDate.getMonth() + 1
                        ).padStart(2, "0");
                        const day = String(
                            parsedDate.getDate()
                        ).padStart(2, "0");

                        formattedDate = `${ year } -${ month } -${ day }`;
                    }
                }
            }

            setInputValue(
                card,
                ".passenger-date-of-birth",
                formattedDate
            );

            // Copy the returned identity numbers into search fields
            setInputValue(
                card,
                ".search-passport",
                getValue(passenger, "passportNumber", "PassportNumber")
            );

            setInputValue(
                card,
                ".search-national-id",
                getValue(passenger, "nationalID", "NationalID")
            );

            showMessage(
                card,
                "Passenger information loaded successfully.",
                "success"
            );
        }
        catch (error) {
            console.error("Passenger search error:", error);

            showMessage(
                card,
                "An error occurred while searching. Please try again.",
                "danger"
            );
        }
        finally {
            if (searchButton) {
                searchButton.disabled = false;
                searchButton.textContent = "Search";
            }
        }
    }

    // =========================================================
    // Search Button - No Form Submission
    // =========================================================

    form.addEventListener("click", function (event) {

        const button = event.target.closest(".search-passenger-btn");

        if (!button || !form.contains(button)) {
            return;
        }

        event.preventDefault();

        const card = button.closest(".passenger-card");

        if (card) {
            searchPassenger(card);
        }
    });

    // =========================================================
    // Enter Key - Search Instead of Submit
    // =========================================================

    form.addEventListener("keydown", function (event) {

        const input = event.target.closest(
            ".search-passport, .search-national-id"
        );

        if (!input || event.key !== "Enter") {
            return;
        }

        event.preventDefault();

        const card = input.closest(".passenger-card");

        if (card) {
            searchPassenger(card);
        }
    });

    // =========================================================
    // Clear Passenger
    // =========================================================

    form.addEventListener("click", function (event) {

        const button = event.target.closest(".clear-passenger-btn");

        if (!button || !form.contains(button)) {
            return;
        }

        event.preventDefault();

        const card = button.closest(".passenger-card");

        if (!card) return;

        const fields = [
            ".selected-passenger-id",
            ".passenger-first-name",
            ".passenger-middle-name",
            ".passenger-last-name",
            ".passenger-passport",
            ".passenger-national-id",
            ".passenger-phone",
            ".passenger-email",
            ".passenger-date-of-birth",
            ".search-passport",
            ".search-national-id"
        ];

        fields.forEach(function (selector) {
            setInputValue(card, selector, "");
        });

        const messageBox = card.querySelector(".passenger-message");

        if (messageBox) {
            messageBox.remove();
        }
    });

});
