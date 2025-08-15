document.addEventListener('DOMContentLoaded', function () {
    const searchForm = document.getElementById('rideSearchForm');
    if (!searchForm) return;

    function filterRides() {
        const from = document.getElementById('fromLocation').value.toLowerCase().trim();
        const to = document.getElementById('toLocation').value.toLowerCase().trim();
        const radius = parseFloat(document.getElementById('radius').value);
        const date = document.getElementById('rideDate').value;
        const time = document.getElementById('rideTime').value;
        const priceMin = parseFloat(document.getElementById('priceMin').value);
        const priceMax = parseFloat(document.getElementById('priceMax').value);
        const seats = parseInt(document.getElementById('rideSeats').value);
        const vehicle = document.getElementById('vehicleType').value.toLowerCase();
        const sortOption = document.getElementById('sortOption').value;

        const cards = Array.from(document.querySelectorAll('#rideResults .ride-card'));
        let filtered = cards.filter(card => {
            const origin = (card.dataset.origin || '').toLowerCase();
            const destination = (card.dataset.destination || '').toLowerCase();
            const price = parseFloat(card.dataset.price);
            const cardDate = card.dataset.date;
            const cardTime = card.dataset.time;
            const cardSeats = parseInt(card.dataset.seats);
            const cardVehicle = (card.dataset.vehicle || '').toLowerCase();
            const distance = parseFloat(card.dataset.distance);

            if (from && !origin.includes(from)) return false;
            if (to && !destination.includes(to)) return false;
            if (!isNaN(radius) && distance > radius) return false;
            if (date && cardDate !== date) return false;
            if (time && cardTime !== time) return false;
            if (!isNaN(priceMin) && price < priceMin) return false;
            if (!isNaN(priceMax) && price > priceMax) return false;
            if (!isNaN(seats) && cardSeats < seats) return false;
            if (vehicle && cardVehicle !== vehicle) return false;
            return true;
        });

        filtered.sort((a, b) => {
            switch (sortOption) {
                case 'price':
                    return parseFloat(a.dataset.price) - parseFloat(b.dataset.price);
                case 'time':
                    return a.dataset.time.localeCompare(b.dataset.time);
                case 'rating':
                    return parseFloat(b.dataset.rating) - parseFloat(a.dataset.rating);
                case 'distance':
                    return parseFloat(a.dataset.distance) - parseFloat(b.dataset.distance);
                default:
                    return 0;
            }
        });

        const container = document.getElementById('rideResults');
        cards.forEach(c => c.style.display = 'none');
        filtered.forEach(c => {
            c.style.display = '';
            container.appendChild(c);
        });

        const noResults = document.getElementById('noResultsMessage');
        if (noResults) {
            if (filtered.length === 0) {
                noResults.style.display = '';
            } else {
                noResults.style.display = 'none';
            }
        }
    }

    searchForm.addEventListener('submit', function (e) {
        e.preventDefault();
        filterRides();
    });
});
