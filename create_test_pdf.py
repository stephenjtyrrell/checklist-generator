#!/usr/bin/env python3
"""
Simple script to create a test PDF with checklist content for testing the checklist generator
"""

from reportlab.lib.pagesizes import letter
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Flowable
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.lib.enums import TA_LEFT, TA_CENTER
import os

def create_test_pdf():
    # Create the PDF file
    filename = "test_checklist.pdf"
    doc = SimpleDocTemplate(filename, pagesize=letter,
                          rightMargin=72, leftMargin=72,
                          topMargin=72, bottomMargin=18)
    
    # Container for the 'Flowable' objects
    story = []
    
    # Get default styles
    styles = getSampleStyleSheet()
    
    # Create custom styles
    title_style = ParagraphStyle(
        'CustomTitle',
        parent=styles['Heading1'],
        fontSize=18,
        spaceAfter=30,
        alignment=TA_CENTER,
    )
    
    heading_style = ParagraphStyle(
        'CustomHeading',
        parent=styles['Heading2'],
        fontSize=14,
        spaceAfter=12,
        spaceBefore=20,
    )
    
    normal_style = styles['Normal']
    
    # Add content
    story.append(Paragraph("Document Compliance Checklist", title_style))
    story.append(Spacer(1, 12))
    
    story.append(Paragraph("Project Setup Requirements", heading_style))
    story.append(Paragraph("• Verify project charter has been approved by stakeholders", normal_style))
    story.append(Paragraph("• Confirm budget allocation is sufficient for project scope", normal_style))
    story.append(Paragraph("• Ensure all team members have signed confidentiality agreements", normal_style))
    story.append(Paragraph("• Check that project timeline aligns with business objectives", normal_style))
    story.append(Spacer(1, 12))
    
    story.append(Paragraph("Documentation Requirements", heading_style))
    story.append(Paragraph("• Technical specifications document must be completed", normal_style))
    story.append(Paragraph("• User requirements have been gathered and documented", normal_style))
    story.append(Paragraph("• Risk assessment matrix has been created and reviewed", normal_style))
    story.append(Paragraph("• Quality assurance plan is in place and approved", normal_style))
    story.append(Spacer(1, 12))
    
    story.append(Paragraph("Compliance and Legal", heading_style))
    story.append(Paragraph("• Data protection impact assessment completed", normal_style))
    story.append(Paragraph("• GDPR compliance measures have been implemented", normal_style))
    story.append(Paragraph("• Security audit has been conducted and passed", normal_style))
    story.append(Paragraph("• Legal review of all contracts and agreements", normal_style))
    story.append(Spacer(1, 12))
    
    story.append(Paragraph("Testing and Validation", heading_style))
    story.append(Paragraph("• Unit tests have been written and executed", normal_style))
    story.append(Paragraph("• Integration testing completed successfully", normal_style))
    story.append(Paragraph("• User acceptance testing scheduled and planned", normal_style))
    story.append(Paragraph("• Performance benchmarks have been established", normal_style))
    story.append(Spacer(1, 12))
    
    story.append(Paragraph("Deployment Checklist", heading_style))
    story.append(Paragraph("• Production environment is configured and ready", normal_style))
    story.append(Paragraph("• Backup and disaster recovery procedures are tested", normal_style))
    story.append(Paragraph("• Monitoring and alerting systems are operational", normal_style))
    story.append(Paragraph("• Documentation for end users has been prepared", normal_style))
    story.append(Paragraph("• Support team has been trained on the new system", normal_style))
    
    # Build PDF
    doc.build(story)
    print(f"✅ Test PDF created: {filename}")
    print(f"📍 Location: {os.path.abspath(filename)}")
    return filename

if __name__ == "__main__":
    create_test_pdf()
