import * as React from 'react';
import { useEffect, useRef } from 'react';
import { DocumentEditorContainerComponent , Ribbon } from '@syncfusion/ej2-react-documenteditor';
import "./index.css";
DocumentEditorContainerComponent.Inject(Ribbon);
let hostUrl = "http://localhost:5257/api/documenteditor/";
// tslint:disable:max-line-length
const Exporting = () => {
    const container = useRef(null);
    const defaultSFDT = `{
        "sections": [{
        "blocks": [{
            "inlines": [{
            "text": "Welcome to Syncfusion Document Editor!",
            "characterFormat": {
                "bold": true,
                "fontSize": 14
            }
            }]
        }]
        }]
    }`;

    useEffect(() => {
        if (container.current) {
        container.current.documentEditor.open(defaultSFDT); // Empty document
        container.current.documentEditor.documentName = 'Exporting';
        container.current.documentEditor.focusIn();
        }
    }, []);
    
    
    // Ribbon File tab Export menu (for toolbarMode: 'Ribbon')
    const ribbonExportItems = [
        { text: 'Word Document (*.docx)', id: 'docx' },
        { text: 'Syncfusion Document Text (*.sfdt)', id: 'sfdt' },
        { text: 'Plain Text (*.txt)', id: 'text' },
        { text: 'Word Template (*.dotx)', id: 'dotx' },
        { text: 'PDF (*.pdf)', id: 'pdf' },
        { text: 'HyperText Markup Language (*.html)', id: 'html' },
        { text: 'OpenDocument Text (*.odt)', id: 'odt' },
        { text: 'Markdown (*.md)', id: 'md' },
        { text: 'Rich Text Format (*.rtf)', id: 'rtf' },
        { text: 'Word XML Document (*.xml)', id: 'wordml' },
    ];
    const fileMenuItems = [
        'New',
        'Open',
        { text: 'Export', id: 'export', iconCss: 'e-icons e-export', items: ribbonExportItems },
    ];

    // Common export handler used by toolbar (ListView) and Ribbon menu
    const handleExportById = (value) => {
        switch (value) {
            case 'docx':
                container.current.documentEditor.save('Sample', 'Docx');
                break;
            case 'sfdt':
                container.current.documentEditor.save('Sample', 'Sfdt');
                break;
            case 'text':
                container.current.documentEditor.save('Sample', 'Txt');
                break;
            case 'dotx':
                container.current.documentEditor.save('Sample', 'Dotx');
                break;
            case 'pdf':
                formatSave('Pdf');
                break;
            case 'html':
                formatSave('Html');
                break;
            case 'odt':
                formatSave('Odt');
                break;
            case 'md':
                formatSave('Md');
                break;
            case 'rtf':
                formatSave('Rtf');
                break;
            case 'wordml':
                formatSave('Xml');
                break;
            default:
                break;
        }
    };

    const onFileMenuItemClick = (args) => {
        if (args && args.item && args.item.id) {
            // Ignore parent Export click; only handle child items
            if (args.item.id !== 'export') {
                handleExportById(args.item.id);
            }
        }
    }
    
    function formatSave(type) {
        let format = type;
        let url = container.current.documentEditor.serviceUrl + 'Export';
        let fileName = container.current.documentEditor.documentName;
        let http = new XMLHttpRequest();
        http.open('POST', url);
        http.setRequestHeader('Content-Type', 'application/json;charset=UTF-8');
        // Set the responseType to 'blob' to handle binary data
        http.responseType = 'blob';
        // Prepare data to send
        let sfdt = {
            Content: container.current.documentEditor.serialize(),
            Filename: fileName,
            Format: '.' + format
        };
        // Set up event listener for the response
        http.onload = function () {
            if (http.status === 200) {
                // Handle the response blob here
                let responseData = http.response;
                // Create a Blob URL for the response data
                let blobUrl = URL.createObjectURL(responseData);
                // Create a link element and trigger the download
                let downloadLink = document.createElement('a');
                downloadLink.href = blobUrl;
                downloadLink.download = fileName + '.' + (format).toLowerCase();
                document.body.appendChild(downloadLink);
                downloadLink.click();
                // Cleanup: Remove the link and revoke the Blob URL
                document.body.removeChild(downloadLink);
                URL.revokeObjectURL(blobUrl);
            }
            else {
                // Handle errors
                console.error('Request failed with status:', http.status);
            }
        };
        // Send the request with JSON.stringify(sfdt) as the request body
        http.send(JSON.stringify(sfdt));
    }

    return (<div className="control-pane">
            <div className="control-section">
                <div id="documenteditor_container_body">
                    <DocumentEditorContainerComponent
                        id="container"
                        ref={container}
                        // style={{ display: 'block' }}
                        height={'690px'}
                        toolbarMode="Ribbon" 
                        ribbonLayout="Classic"
                        serviceUrl={hostUrl}
                        enableToolbar={true}
                        locale="en-US" 
                        fileMenuItems={fileMenuItems}
                        fileMenuItemClick={onFileMenuItemClick}
                    />
                </div>
            </div>
        </div>);
};
export default Exporting;

