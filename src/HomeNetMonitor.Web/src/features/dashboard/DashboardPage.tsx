import { useEffect, useState } from "react";

type HealthResponse = {
    status: string;
    timestampUtc: string;
};

function DashboardPage() {
    const [health, setHealth] = useState<HealthResponse | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        async function loadHealth() {
            try{
                const response = await fetch(
                    `${import.meta.env.VITE_API_BASE_URL}/api/health`,
                );

                if(!response.ok) {
                    throw new Error(`API returned ${response.status}`);
                }
                const data: HealthResponse = await response.json()
                setHealth(data);
            }catch(err) {
                setError(err instanceof Error ? err.message: "Unknown Error");
            }
        }

        loadHealth();
    }, []);

    return (
        <section className="page">
            <div className="page-heading">
                <p className="page-heading__eyebrow">OVERVIEW</p>

                <h2>Your network at a glance</h2>

                <p>HomeNEtMonitor will surface network health, connected devices, activity, and security signals here.</p>
            </div>

            <div className="status-card">
                <div>
                    <p className="status-card__label">API</p>

                    <h3>{error
                        ? `Connection Failed: ${error}`
                        : health
                            ? `API is ${health.status}`
                            : "Checkin API connection..."}
                    </h3>
                </div>

                <span className="status-badge">
                    {error ?"ERROR" : health ? "ONLINE" : "CHECKING"}
                </span>

            </div>

        </section>
    )
}

export default DashboardPage