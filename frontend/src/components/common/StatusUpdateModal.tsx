import React, { useState } from 'react';
import { CandidateStatusUpdate, getStatusColor } from '../../types/candidate';

interface StatusUpdateModalProps {
  isOpen: boolean;
  onClose: () => void;
  candidateId: string;
  candidateName: string;
  currentStatus: string;
  availableStatuses: string[];
  onStatusUpdate: (candidateId: string, statusUpdate: CandidateStatusUpdate) => Promise<void>;
}

const StatusUpdateModal: React.FC<StatusUpdateModalProps> = ({
  isOpen,
  onClose,
  candidateId,
  candidateName,
  currentStatus,
  availableStatuses,
  onStatusUpdate
}) => {
  const [selectedStatus, setSelectedStatus] = useState(currentStatus);
  const [changeReason, setChangeReason] = useState('');
  const [isUpdating, setIsUpdating] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedStatus === currentStatus) {
      onClose();
      return;
    }

    setIsUpdating(true);
    try {
      await onStatusUpdate(candidateId, {
        newStatus: selectedStatus,
        changeReason: changeReason.trim() || undefined,
        changedBy: 'User' // In a real app, this would be the current user
      });
      onClose();
    } catch (error) {
      console.error('Failed to update status:', error);
      // In a real app, show error notification
    } finally {
      setIsUpdating(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg w-full max-w-md">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex justify-between items-center">
            <h3 className="text-lg font-semibold text-gray-900">Update Status</h3>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 transition-colors"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <p className="text-sm text-gray-600 mt-1">{candidateName}</p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="p-6">
          {/* Current Status */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Current Status
            </label>
            <span className={`inline-flex px-3 py-1 text-sm rounded-full ${getStatusColor(currentStatus)}`}>
              {currentStatus}
            </span>
          </div>

          {/* New Status */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              New Status
            </label>
            <select
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              {availableStatuses.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>

          {/* Change Reason */}
          <div className="mb-6">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Reason for Change (Optional)
            </label>
            <textarea
              value={changeReason}
              onChange={(e) => setChangeReason(e.target.value)}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter reason for status change..."
            />
          </div>

          {/* Buttons */}
          <div className="flex gap-3">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isUpdating || selectedStatus === currentStatus}
              className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isUpdating ? 'Updating...' : 'Update Status'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default StatusUpdateModal;