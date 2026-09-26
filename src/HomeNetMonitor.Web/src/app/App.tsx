import {Route, Routes} from "react-router";

import AppShell from "./layout/AppShell";
import DashboardPage from "../features/dashboard/DashboardPage";
import PlaceholderPage from "../shared/components/PlaceholderPage";

function App() {
    return (
        <Routes>

            <Route element={<AppShell />}>
                <Route index element={<DashboardPage />}/>

                <Route 
                path="devices" 
                element={<PlaceholderPage 
                title="Devices" 
                description ="Discover and inspect devices connected to your network."/>} 
                />

                <Route 
                path="activity" 
                element={<PlaceholderPage 
                title="Activity" 
                description="Inspect DNS request and network activity."/>}
                />

                <Route
                path="topology"
                element={<PlaceholderPage
                title="Topology"
                description="Visualize how devices are connected across your network."/>}
                />

            </Route>
        </Routes>
    )
}

export default App;