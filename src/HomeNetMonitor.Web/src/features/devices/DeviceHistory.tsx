import { useEffect, useState } from "react";

type DeviceObservation = {
    id: number;
    observedAtUtc: string;
    ipAddress: string;
    respondedToPing: boolean;
    roundtripTimeMs: number | null;
};

type DeviceHistoryProps = {
    deviceId: number;
};

function DeviceHistory({ deviceId }: DeviceHistoryProps) {
    const [observations, setObservations] = useState<DeviceObservation[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const controller = new AbortController();

        async function loadHistory() {
            setLoading(true);
            setError(null);
            setObservations([]);

            try {
                const response = await fetch(
                    `${import.meta.env.VITE_API_BASE_URL}/api/network/devices/${deviceId}/history`,
                    { signal: controller.signal }
                );

                if (!response.ok) {
                    throw new Error(`API returned ${response.status}`);
                }

                const data: DeviceObservation[] = await response.json();
                setObservations(data);
            } catch (err) {
                if (controller.signal.aborted) return;

                setError(
                    err instanceof Error
                        ? err.message
                        : "Unable to load device history."
                );
            } finally {
                if (!controller.signal.aborted) {
                    setLoading(false);
                }
            }
        }

        loadHistory();

        return () => controller.abort();
    }, [deviceId]);

    if (loading) {
        return <p className="devices-message">Loading device history...</p>;
    }

    if (error) {
        return <p className="devices-message devices-message--error">{error}</p>;
    }

    return (
        <div className="device-history">
            <h4>Observation History</h4>

            {observations.length === 0 ? (
                <p>No observations recorded yet.</p>
            ) : (
                <div className="device-history__list">
                    {observations.map((observation) => (
                        <div
                            className="device-history__row"
                            key={observation.id}
                        >
                            <span>
                                {new Date(
                                    observation.observedAtUtc + "Z"
                                ).toLocaleString()}
                            </span>

                            <span>
                                {observation.roundtripTimeMs !== null
                                    ? `${observation.roundtripTimeMs} ms`
                                    : "No ping"}
                            </span>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

export default DeviceHistory;