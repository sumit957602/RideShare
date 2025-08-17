document.addEventListener('DOMContentLoaded', () => {
    const acceptedRidesTab = document.querySelector('[data-bs-target="#v-pills-acceptedrides"]');
    const tableBody = document.getElementById('acceptedRidesTableBody');
    let loaded = false;

    function renderNoData(message) {
        tableBody.innerHTML = `<tr><td colspan="5">${message}</td></tr>`;
    }

    function loadAcceptedRides() {
        if (loaded || !driverId) return;

        fetch(`/api/RideBookings/driver/${driverId}`)
            .then(response => {
                if (!response.ok) throw new Error('Failed to fetch accepted rides');
                return response.json();
            })
            .then(bookings => {
                tableBody.innerHTML = '';
                if (!Array.isArray(bookings) || bookings.length === 0) {
                    renderNoData('No accepted rides.');
                    return;
                }
                bookings.forEach(b => {
                    const row = document.createElement('tr');
                    row.innerHTML =
                        `<td>${b.passengerId}</td>` +
                        `<td>${b.origin} <i class="bi bi-arrow-right"></i> ${b.destination}</td>` +
                        `<td>${b.bookedSeats}</td>` +
                        `<td>${b.pickupLocation}</td>` +
                        `<td>${b.dropLocation}</td>`;
                    tableBody.appendChild(row);
                });
                loaded = true;
            })
            .catch(err => {
                console.error(err);
                renderNoData('Failed to load accepted rides.');
            });
    }

    if (acceptedRidesTab) {
        acceptedRidesTab.addEventListener('shown.bs.tab', loadAcceptedRides);
    }
});
