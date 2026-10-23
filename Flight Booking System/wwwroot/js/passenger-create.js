
"use strict";

document.addEventListener("DOMContentLoaded", function () {
    const messageBox = document.getElementById("searchMessage");
    const passengerForm = document.getElementById("passengerForm");

    const searchUrl =
        passengerForm instanceof HTMLFormElement
            ? passengerForm.dataset.searchUrl
            : undefined;

    /**
     * Display a message to the user.
     * @param {string} message
     * @param {string} type
     * @returns {void}
     */
    function showMessage(message, type) {
        if (!(messageBox instanceof HTMLElement)) {
            return;
        }

        messageBox.className = "alert alert-" + type;
        messageBox.textContent = message;
        messageBox.classList.remove("d-none");
    }

    /**
     * Set the value of an input inside a passenger card.
     * @param {HTMLElement} card
     * @param {string} selector
     * @param {string | number | null | undefined} value
     * @returns {void}
     */
    function setValue(card, selector, value) {
        const element = card.querySelector(selector);

        if (
            element instanceof HTMLInputElement ||
            element instanceof HTMLSelectElement ||
            element instanceof HTMLTextAreaElement
        ) {
            element.value = value == null ? "" : String(value);
        }
    }

    /**
     * Read a value from an input inside a passenger card.
     * @param {HTMLElement} card
     * @param {string} selector
     * @returns {string}
     */
    function getInputValue(card, selector) {
        const element = card.querySelector(selector);

        if (
            element instanceof HTMLInputElement ||
            element instanceof HTMLSelectElement ||
            element instanceof HTMLTextAreaElement
        ) {
            return element.value.trim();
        }

        return "";
    }

    /**
     * Clear the passenger details for one seat.
     * @param {HTMLElement} card
     * @returns {void}
     */
    function clearPassenger(card) {
        setValue(card, ".selected-passenger-id", "");
        setValue(card, ".passenger-first-name", "");
        setValue(card, ".passenger-middle-name", "");
        setValue(card, ".passenger-last-name", "");
        setValue(card, ".passenger-passport", "");
        setValue(card, ".passenger-national-id", "");
        setValue(card, ".passenger-phone", "");
        setValue(card, ".passenger-email", "");
        setValue(card, ".passenger-date-of-birth", "");
        setValue(card, ".search-passport", "");
        setValue(card, ".search-national-id", "");
    }

    /**
     * Search for an existing passenger and fill the form.
     * @param {HTMLElement} card
     * @returns {Promise<void>}
     */
    async function searchPassenger(card) {
        if (!searchUrl) {
            showMessage(
                "Search URL is missing. Check the form data-search-url attribute.",
                "danger"
            );
            return;
        }

        const passportNumber = getInputValue(
            card,
            ".search-passport"
        );

        const nationalID = getInputValue(
            card,
            ".search-national-id"
        );

        if (!passportNumber && !nationalID) {
            showMessage(
                "Enter a passport number or national ID.",
                "warning"
            );
            return;
        }

        const url = new URL(searchUrl, window.location.origin);

        if (passportNumber) {
            url.searchParams.set("passportNumber", passportNumber);
        }

        if (nationalID) {
            url.searchParams.set("nationalID", nationalID);
        }

        const searchButton = card.querySelector(
            ".search-passenger-btn"
        );

        if (searchButton instanceof HTMLButtonElement) {
            searchButton.disabled = true;
            searchButton.textContent = "Searching...";
        }

        try {
            const response = await fetch(url.toString(), {
                method: "GET",
                headers: {
                    "Accept": "application/json"
                }
            });

            if (!response.ok) {
                showMessage(
                    "Unable to search for this passenger.",
                    "danger"
                );
                return;
            }

            /** @type {unknown} */
            const rawData = await response.json();

            if (
                typeof rawData !== "object" ||
                rawData === null ||
                !("id" in rawData)
            ) {
                showMessage(
                    "No existing passenger was found.",
                    "warning"
                );
                return;
            }

            const data = /** @type {{
                id: number | string,
                firstName?: string | null,
                middleName?: string | null,
                lastName?: string | null,
                passportNumber?: string | null,
                nationalID?: string | null,
                phone?: string | null,
                email?: string | null,
                dateOfBirth?: string | null
            }} */ (rawData);

            if (data.id == null || String(data.id) === "") {
                showMessage(
                    "No existing passenger was found.",
                    "warning"
                );
                return;
            }

            setValue(card, ".selected-passenger-id", data.id);
            setValue(card, ".passenger-first-name", data.firstName);
            setValue(card, ".passenger-middle-name", data.middleName);
            setValue(card, ".passenger-last-name", data.lastName);
            setValue(card, ".passenger-passport", data.passportNumber);
            setValue(card, ".passenger-national-id", data.nationalID);
            setValue(card, ".passenger-phone", data.phone);
            setValue(card, ".passenger-email", data.email);

            if (data.dateOfBirth) {
                const date = new Date(data.dateOfBirth);

                if (!Number.isNaN(date.getTime())) {
                    const formattedDate =
                        date.getFullYear() + "-" +
                        String(date.getMonth() + 1).padStart(2, "0") + "-" +
                        String(date.getDate()).padStart(2, "0");

                    setValue(
                        card,
                        ".passenger-date-of-birth",
                        formattedDate
                    );
                } else {
                    setValue(
                        card,
                        ".passenger-date-of-birth",
                        data.dateOfBirth
                    );
                }
            } else {
                setValue(card, ".passenger-date-of-birth", "");
            }

            showMessage(
                "Passenger information loaded successfully.",
                "success"
            );
        } catch (error) {
            showMessage(
                "An error occurred while searching for the passenger.",
                "danger"
            );

            console.error("Passenger search failed:", error);
        } finally {
            if (searchButton instanceof HTMLButtonElement) {
                searchButton.disabled = false;
                searchButton.textContent = "Search";
            }
        }
    }

    document.querySelectorAll(".passenger-card").forEach(
        function (element) {
            if (!(element instanceof HTMLElement)) {
                return;
            }

            const card = element;

            const searchButton = card.querySelector(
                ".search-passenger-btn"
            );

            const clearButton = card.querySelector(
                ".clear-passenger-btn"
            );

            if (searchButton instanceof HTMLButtonElement) {
                searchButton.addEventListener("click", function () {
                    void searchPassenger(card);
                });
            }

            if (clearButton instanceof HTMLButtonElement) {
                clearButton.addEventListener("click", function () {
                    clearPassenger(card);
                });
            }

            const searchPassport = card.querySelector(
                ".search-passport"
            );

            const searchNationalId = card.querySelector(
                ".search-national-id"
            );

            [searchPassport, searchNationalId].forEach(
                function (element) {
                    if (element instanceof HTMLInputElement) {
                        element.addEventListener(
                            "keydown",
                            function (event) {
                                if (event.key === "Enter") {
                                    event.preventDefault();
                                    void searchPassenger(card);
                                }
                            }
                        );
                    }
                }
            );
        }
    );
});
