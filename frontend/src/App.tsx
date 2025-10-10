import * as React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import DashboardPage from './pages/Dashboard';
import CandidatesPage from './pages/CandidatesPage';
import SkillsPage from './pages/SkillsPage';
import Settings from './pages/Settings';
import './assets/styles/index.css';

const App: React.FC = () => {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/candidates" element={<CandidatesPage />} />
        <Route path="/skills" element={<SkillsPage />} />
        <Route path="/settings" element={<Settings />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Router>
  );
};

export default App;