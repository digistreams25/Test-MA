import { Router } from 'express';

export const auditRouter = Router();

// Mock audit data
const auditEntries = [
  {
    id: 'AUD-001',
    timestamp: '2025-12-01T16:45:00Z',
    clashId: 'CLH-003',
    proposalId: 'PROP-003',
    optionApplied: 'OPT-1: Lower pipe',
    approvedBy: 'bim-coordinator@example.com',
    elementsModified: [
      {
        elementId: 'E-WTR-002',
        property: 'Invert Elevation',
        oldValue: '183.800',
        newValue: '183.480',
        unit: 'm',
      },
      {
        elementId: 'E-WTR-002',
        property: 'Crown Elevation',
        oldValue: '183.950',
        newValue: '183.630',
        unit: 'm',
      },
    ],
  },
];

auditRouter.get('/:projectId', (req, res) => {
  res.json(auditEntries);
});

auditRouter.get('/:projectId/export', (req, res) => {
  const format = req.query.format || 'csv';
  // In production: generate CSV or PDF from audit entries
  if (format === 'csv') {
    res.setHeader('Content-Type', 'text/csv');
    res.setHeader('Content-Disposition', 'attachment; filename=audit-log.csv');
    const header = 'Date,Clash ID,Proposal,Option,Approved By,Elements Modified\n';
    const rows = auditEntries
      .map(
        (e) =>
          `${e.timestamp},${e.clashId},${e.proposalId},${e.optionApplied},${e.approvedBy},${e.elementsModified.length}`
      )
      .join('\n');
    res.send(header + rows);
  } else {
    res.json({ message: 'PDF export not yet implemented' });
  }
});
