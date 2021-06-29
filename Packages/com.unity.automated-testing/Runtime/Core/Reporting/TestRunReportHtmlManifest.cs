public static class TestRunReportHtmlManiest
{

	public static readonly string TEST_RUN_REPORT_HTML_TEMPLATE = @"
		<head>
			<script type='text/javascript' src='https://www.gstatic.com/charts/loader.js'></script>
			<script src='https://code.jquery.com/jquery-3.6.0.min.js' integrity='sha256-/xUj+3OJU5yExlq6GSYGSHk7tPXikynS7ogEvDej/m4=' crossorigin='anonymous'></script>
			<script type='text/javascript'>
				google.charts.load('current', {'packages':['corechart']});
				google.charts.setOnLoadCallback(drawChart);
				function drawChart() {
					let statusData = $('.pie-chart-data').val().split(',');
					let chartData = [];
					for(let x = 0; x < statusData.length; x++) {
						let slice = statusData[x].split('|');
						let sliceName = slice[0];
						let sliceCount = x == 0 ? slice[1] : parseInt(slice[1]); // x == 0 array are the column names. Cannot be a number.
						chartData.push([sliceName,sliceCount]);
					}
					var data = google.visualization.arrayToDataTable(chartData);
					var options = {
					  colors: ['green', 'orange', 'red', 'grey'],
					  legend: { position: 'left', alignment: 'center' },
					  chartArea: {width: 275, height: 275}
					};
					var chart = new google.visualization.PieChart(document.getElementById('piechart'));
					function selectHandler() {
						var selectedItem = chart.getSelection()[0];
						if (selectedItem) {
							PieChartSelect(data.getValue(selectedItem.row, 0));
						}
					}
				google.visualization.events.addListener(chart, 'select', selectHandler);
				chart.draw(data, options);
			  }
			</script>
			<script>
				var INFO_CHAR = '&#128712;';
				var NOT_RUN_CHAR = '―';
				var ERROR_CHAR = '✘';
				var CHECK_MARK_CHAR = '✔';
				var WARNING_CHAR = '⚠';
				function GetStatusIndicator(status)
				{
					switch (status)
					{
						case 'Fail':
							return ERROR_CHAR;
						case 'NotRun':
							return NOT_RUN_CHAR;
						case 'Pass':
							return CHECK_MARK_CHAR;
						case 'Warning':
							return WARNING_CHAR;
						default:
							return INFO_CHAR.ToCharArray().First();
					}
				}

				function GetStatusClass(status)
				{
					switch (status)
					{
						case 'Fail':
							return 'fail';
						case 'Pass':
							return 'pass';
						case 'Warning':
							return 'warning';
						case 'NotRun':
						default:
							return 'notrun';
					}
				}

				function GetLogTypeIndicator(logType)
				{
					switch (logType)
					{
						case 'Log':
							return INFO_CHAR;
						case 'Error':
						case 'Exception':
							return ERROR_CHAR;
						case 'Warning':
							return WARNING_CHAR;
						default:
							return NOT_RUN_CHAR;
					}
				}
		
				function GetLogTypeClass(logType)
				{
					switch (logType)
					{
						case 'Error':
						case 'Exception':
							return 'error';
						case 'Warning':
							return 'warn';
						case 'Log':
						default:
							return 'log';
					}
				}
		
				function ToggleDetails(el) {
					if($(el.nextElementSibling).is(':visible')) {
						$(el.nextElementSibling).slideUp(400);
					} else {
						$(el.nextElementSibling).slideDown(400);
					}		
				}
		
				function ShowStackTrace(el) {
					let stacktrace = $(el).find('.console-log-stacktrace').val();
					if(stacktrace.length == 0) return;
					let modal = $('.modal-popup');
					let modal_background = $('.modal-background');
					modal.css('display', 'block');
					modal.find('.stacktrace-value').text(stacktrace);
					modal_background.css('display', 'block');
					modal_background.addClass('modal-background-show').css('animation-direction', 'normal');
					modal.addClass('modal-show').css('animation-direction', 'normal');
				}
		
				var last_selected = '';
				function PieChartSelect(selection) {
					var piechart_message_tooltip = $('.piechart-message-tooltip');
					var message_area = $('.piechart-messages');
					var piechart_errors = $('.piechart-error');
					var piechart_warnings = $('.piechart-warning');
					var piechart_logs = $('.piechart-log');
					var recording_toggles = $('.recording-toggle');
					piechart_errors.hide();
					piechart_warnings.hide();
					piechart_logs.hide();
					let status_indicator_pass = $('.status-indicator.pass:not(.step-square)');
					let status_indicator_fail = $('.status-indicator.fail:not(.step-square)');
					let status_indicator_warning = $('.status-indicator.warn:not(.step-square)');
					let status_indicator_notrun = $('.status-indicator.notrun:not(.step-square)');
			
					if(last_selected == selection) {
						status_indicator_pass.parent().show();
						status_indicator_fail.parent().show();
						status_indicator_warning.parent().show();
						status_indicator_notrun.parent().show();
						piechart_logs.show();
						piechart_errors.show();
						piechart_warnings.show();
						recording_toggles.show();
						last_selected = '';
						return;
					}
			
					switch(selection.toLowerCase()) {
						case 'pass':
							status_indicator_pass.parent().show();
							status_indicator_fail.parent().next().hide();
							status_indicator_fail.parent().hide();
							status_indicator_warning.parent().next().hide();
							status_indicator_warning.parent().hide();
							status_indicator_notrun.parent().next().hide();
							status_indicator_notrun.parent().hide();
							piechart_logs.show()
							break;
						case 'fail':
							status_indicator_pass.parent().next().hide();
							status_indicator_pass.parent().hide();
							status_indicator_fail.parent().show();
							status_indicator_warning.parent().next().hide();
							status_indicator_warning.parent().hide();				
							status_indicator_notrun.parent().next().hide();
							status_indicator_notrun.parent().hide();
							piechart_errors.show();
							break;
						case 'warning':
							status_indicator_pass.parent().next().hide();
							status_indicator_pass.parent().hide();
							status_indicator_fail.parent().next().hide();
							status_indicator_fail.parent().hide();
							status_indicator_warning.parent().show();				
							status_indicator_notrun.parent().next().hide();
							status_indicator_notrun.parent().hide();
							piechart_warnings.show();
							break;
						case 'not run':
							status_indicator_pass.parent().next().hide();
							status_indicator_pass.parent().hide();
							status_indicator_fail.parent().next().hide();
							status_indicator_fail.parent().hide();
							status_indicator_warning.parent().next().hide();
							status_indicator_warning.parent().hide();				
							status_indicator_notrun.parent().show();
							break;
					}
					message_area.css('display', 'inline-block');
					piechart_message_tooltip.hide();
					last_selected = selection;
				}
		
				$(function ()  { 
		
					var testResultsJson = JSON.parse($('#test_results').val());
					$('#start_time').text(testResultsJson.RunStartTime);
					$('#run_time').text(testResultsJson.RunTime + ' (s)');
					$('#device_type').text(testResultsJson.DeviceType);
					$('#device_model').text(testResultsJson.DeviceModel);
					$('#device_udid').text(testResultsJson.Udid);
					$('#aspect_ratio').text(testResultsJson.AspectRatio);
					$('#resolution').text(testResultsJson.Resolution);
					$('header-title').text(`${(testResultsJson.isLocalRun ? 'Local' : 'Cloud/CI')} Test Run Report`);
			
					let piechart_logs_html = '';
					for(let a = 0; a < testResultsJson.AllLogs.length; a++) 
					{

						let log = testResultsJson.AllLogs[a];
						let logClass = GetLogTypeClass(log.Type);
						piechart_logs_html += `
							<div class='console-log piechart-${logClass}${(log.StackTrace.Length > 0 ? ' has-stacktrace' : '')}' onclick='ShowStackTrace(this);'>
								<strong>&nbsp;
									<span class='char ${GetLogTypeClass(log.Type)}'>
										${GetLogTypeIndicator(log.Type)}
									</span>
									&nbsp;${(log.CountInARow > 0 ? `[${(log.CountInARow + 1)}]` : '')}
									</strong>
									&nbsp;${log.Message}
									<input type='hidden' class='console-log-stacktrace' value='${log.StackTrace}'/>
							</div>`;

					}
					let passCount, failCount, warningCount, notRunCount;
					passCount = failCount = warningCount = notRunCount = 0;
					for(let t = 0; t < testResultsJson.Tests.length; t++) {
			
						let test = testResultsJson.Tests[t];
						switch(test.Status) {
							case 'Pass':
								passCount++;
								break;
							case 'Fail':
								failCount++;
								break;
							case 'Warning':
								warningCount++;
								break;
							case 'NotRun':
								notRunCount++;
								break;
						}
				
						$('.header-title').text(`${(testResultsJson.IsLocalRun ? 'Local' : 'CI/CD')} Test Run Report`);
				
						let testHtml = `
							<div id='test_${t}_toggle' class='recording-toggle' onclick='ToggleDetails(this);'>
								<div class='status-indicator ${GetStatusClass(test.Status)}'>
									<div>✔</div>
								</div> 
								<div class='recording-name'>
									 ${(test.TestName == null || test.TestName.length == 0 ? test.RecordingName : `${test.TestName}: (${test.RecordingName})`)}
								</div>
							</div>
						`;

						testHtml += `<div id='test_${t}_details_region' class='recording-details-region'>`;
						if(test.Steps.length == 0) {
							testHtml += '<h3 style=\'color: red;\'><em>This test did not have any steps! Automatically failing as there should not be an \'empty\' test.</em></h3>';
						}

						for(let s = 0; s < test.Steps.length; s++) {
				
							let step = test.Steps[s];					
							let statusClass = GetStatusClass(step.Status);
							testHtml += `
								<div class='step-toggle ${statusClass}' onclick='ToggleDetails(this);'>
									<div class='status-indicator step-square ${GetStatusClass(step.Status)}'>
										<div>${GetStatusIndicator(step.Status)}</div>
									</div>
									<div class='recording-name'>
										${step.ActionType} <span class='game-object-heirarchy'>[${step.Scene}: ${step.Name}]</span>
									</div>
								</div>
							`;
							testHtml += `
								 <div class='recording-details'>
									 <h4>Screenshot Before</h4>
									 <div><em>${(step.ScreenshotBefore != null && step.ScreenshotBefore.length > 0 ? `<img class='screenshot' src='${step.ScreenshotBefore}'/>` : 'N/A')}</em></div>
									 <h4>Screenshot After</h4>
									 <div><em>${(step.ScreenshotAfter != null && step.ScreenshotAfter.length > 0 ? `<img class='screenshot' src='${step.ScreenshotAfter}'/>` : 'N/A')}</em></div>
									 <div class='recording-details-data'>
										 <div><strong>Scene:</strong>&nbsp;${step.Scene}</div> 
										 <div><strong>Hierarchy:</strong>&nbsp;${step.Hierarchy}</div> 
										 <div><strong>Coordinates:</strong>&nbsp;${step.Coordinates}</div>
									 </div> 
							`;
							testHtml += '<div class=\'recording-details-logs\'>';
							if(step.Logs.length == 0) {
								testHtml += '<div><strong><em>No logs recorded during this step.</em></strong></div>'
							}
							let stepLogsHtml = '';
							for(let l = 0; l < step.Logs.length; l++) {
					
								let log = step.Logs[l];
								let logClass = GetLogTypeClass(log.Type);
								stepLogsHtml += `
									<div class='console-log {(log.StackTrace.Length > 0 ? ' has-stacktrace' : string.Empty)}' onclick='ShowStackTrace(this);'>
										<strong>
											<span class='char ${GetLogTypeClass(log.Type)}'>
												${GetLogTypeIndicator(log.Type)}
											</span>
											&nbsp;${(log.CountInARow > 0 ? `[${(log.CountInARow + 1)}]` : '')}
										</strong>
										&nbsp;${log.Message}
										<input type='hidden' class='console-log-stacktrace' value='${log.StackTrace}'/>
									</div>`;
					
							}
							testHtml += stepLogsHtml;
							testHtml += '</div>'; // End .recording-details-logs
							testHtml += '</div>'; // End .recording-details
				
						}
						testHtml += '</div>'; // End .recording-details-region
						$('.recordings-container').append(testHtml);
				
					}
			
					$('.pie-chart-data').val(`Status|Count,Pass|${passCount},Warning|${warningCount},Fail|${failCount},Not Run|${notRunCount}}`);
					// Add all logs to summary log region.
					$('.piechart-messages').html(piechart_logs_html);			
			
				});

			</script>
			<style>
				body {
					font-family: sans-serif;
					overflow-x: hidden;
					margin-bottom: 25px;
				}
				.char {
					font-weight: bold;
				}
				.char.error {
					color: red;
				}
				.char.log {
					color: blue;
					font-size: 20px;
				}
				.char.warn {
					color: orange;
				}
				.console-log {
					margin: 5px 0 5px 0;
					padding: 3px 0 5px 0;
					white-space: normal;
					word-break: break-word;
				}
				.game-object-heirarchy {
					font-size: 12px;
					vertical-align: middle;
				}
				.google-visualization-tooltip {
					pointer-events: none;
				}
				.has-stacktrace {
					margin: 7px 0 7px 0;
					background-color: #ffa8b44a;
					border: 1px solid #ffa8b44a;
					border-radius: 6px;
					cursor: pointer;
				}
				.header-logo {
					position: absolute;
					top: 0;
				}
				.header-region {
					width: calc(100% - 125px);
					height: 80px;
					position: absolute;
					background-color: black;
					left: 125px;
					top: 0;
				}
				.header-region::before {
					position: absolute;
					display: inline-block;
					border-top: 40px solid transparent;
					border-left: 40px solid white;
					border-bottom: 40px solid transparent;
					content: '';
				}
				.header-title {
					color: white;
					padding-left: 60px;
					white-space: nowrap;
				}
				.modal-background {
					display: none;
					z-index: 98;
					position: fixed;
					background-color: black;
					width: 100%;
					height: 100%;
					top: 0;
					left: 0;
				}
				.modal-close {
					cursor: pointer;
					position: absolute;
					top: 0;
					right: 0;
					padding: 5px 10px 5px 10px;
					border: 1px solid black;
					border-radius: 6px;
				}
				.modal-popup {
					display: none;
					position: fixed;
					width: 50%;
					height: 50%;
					left: 50%;
					top: 50%;
					transform: translate(-50%, -50%);
					background-color: white;
					border: 2px solid black;
					border-radius: 6px;
				}
				.modal-title {
					width: 150px;
					margin-left: 50%;
					transform: translate(-50%);
				}
				.modal-show {
					z-index: 99;
					animation-fill-mode: forwards;
					animation-name: toggleVisible;
					animation-duration: 1s;
					animation-direction: normal;
				}
				.modal-background-show {
					z-index: 98;
					animation-fill-mode: forwards;
					animation-name: toggleHalfVisible;
					animation-duration: 1s;
					animation-direction: normal;
				}
				#piechart {
					position: relative;
					z-index: 1;
					margin-top: 25px;
					display: inline-block;
					left: -50px;
				}
				.piechart-error {
					background-color: #fff5f2;
					border-radius: 6px;
					padding: 5px;
					cursor: pointer;
				}
				.piechart-messages {
					display: inline-block;
					display: none;
					position: relative;
					padding: 5px;
					z-index: 2;
					overflow-x: hidden;
					height: 200px;
					width: calc(100% - 860px);
					overflow: scroll;
					border: 1px solid black;
					border-radius: 6px;
					margin-left: -75px;
				}
				.piechart-message-tooltip {
					cursor: pointer;
					position: relative;
					z-index: 2;
					left: -75px;
					top: -50px;
					opacity: 0.5;
					font-style: italic;
					display: inline-block;
					width: 300px;
					white-space: pre-wrap;
				}
				.recording-details, .recording-details-region {
					display: none;
					margin: 0 0 25px 25px;
				}
				.recording-details > img {
					width: 100%;
				}
				.recording-details-data {
					margin-top: 20px;
					padding: 10px;
					background-color: #80808038;
					border-radius: 6px;
				}
				.recording-details-logs {
					margin-top: 10px;
					padding: 10px;
					background-color: #80808038;
					border-radius: 6px;
				}
				.recording-toggle, .step-toggle {
					cursor:pointer;
					margin-left: 25px;
					height: 40px;
					width: calc(100% - 15px);
					background-color: black;
					color: white;
					margin-top: 10px;
					white-space: nowrap;
				}
				.recording-toggle::before {
					position: absolute;
					z-index: 0;
					display: inline-block;
					border: 20px solid black;
					border-radius: 50%;
					left: 12px;
					content: '';
				}
				.recordings-container {
					margin-top: 325px;
				}
				.recording-name {
					display: inline-block;
					position: relative;
					color: white;
					font-size: 1.4em;
				}
				.screenshot {
					margin-top: 1px;
					border: 2px solid black;
					border-radius: 5px;
				}
				.stacktrace-value {
					position: absolute;
					top: 50px;
					width: calc(100% - 20px);;
					height: calc(100% - 75px);
					padding: 10px;
					white-space: pre-wrap;
					white-space: -moz-pre-wrap;   
					white-space: -pre-wrap;   
					white-space: -o-pre-wrap;
					word-wrap: break-word;
					overflow-y: scroll;
				}
				.status {
					width: 50px;
					height: 75px;
					display: inline-block;
					vertical-align: middle;
				}
				.status-indicator {
					display: inline-block;
					position: relative;
					z-index: 1;
					top: 6px;
					left: -15px;
					width: 25px;
					height: 25px;
					border: 1px solid black;
					border-radius: 50%;
				}
				.status-indicator.step-square {
					border-radius: 0;
					left: 5px;
				}
				.status-indicator > div {
					display: table;
					margin: 0 auto;
					color: white;
				}
				.status-indicator.fail {
					background-color: red;
				}
				.status-indicator.pass {
					background-color: green;
				}
				.status-indicator.warn {
					background-color: orange;
				}
				.status-indicator.notrun {
					background-color: grey;
					font-weight: bold;
				}
				.status-indicator.notrun > div {
					margin-top: 2.5px;
				}
				.status-item {
					display: block;
					width: 100%;
					margin: 0 auto;
				}
				.status-summary-region {
					position: absolute;
					width: 100%;
					top: 80px;
					white-space: nowrap;
				}
				.step-toggle {
					background-color: grey;
					overflow: hidden;
				}
				.step-toggle > .recording-name {
					margin-left: 20px;
				}
				.step-toggle.pass {
					background-image: linear-gradient(to right, green, black);
				}
				.step-toggle.fail {
					background-image: linear-gradient(to right, red, black);
				}
				.step-toggle.warn {
					background-image: linear-gradient(to right, orange, black);
				}
				.step-toggle.skipped {
					background-image: linear-gradient(to right, grey, black);
				}
				.test-run-data {
					white-space: nowrap;
					padding: 4px;
				}	
				.test-run-data > .data-label {
					font-weight: bold;
					display: inline-block;
					margin-right: 10px;
				}
				.test-run-data > .data-value {
					display: inline-block;
				}
				.test-run-data-region {
					top: -20px;
					position: relative;
					display: inline-block;
					padding: 10px;
					margin-left: 10px;
					border: 2px solid black;
					border-radius: 6px;
					z-index: 2;
				}
				text {
					pointer-events: none;
				}
				::-webkit-scrollbar {
				  width: 20px;
				}
				::-webkit-scrollbar-thumb {
					background-color: black;
					background-clip: content-box;
					border-radius: 25px;
					border: 2px solid transparent;
				}
				::-webkit-scrollbar-track {
					background-color: transparent;
				}
				@media screen and (max-width: 1250px) {
					.piechart-messages {
						display: block !important;
						margin-left: 10px;
						position: relative;
						width: calc(100% - 50px);
					}
					.piechart-message-tooltip {
						display: none;
					}
					.recordings-container {
						margin-top: 575px;
					}
				}
				@media screen and (max-width: 1000px) {
					.modal-popup {
						width: 70%;
						height: 70%;
					}
				}
				@media screen and (max-width: 700px) {
					.modal-popup {
						width: 90%;
						height: 90%;
					}
					#piechart {
						display: block;
						position: relative;
					}
					.test-run-data-region {
						position: relative;
						top: 20;
					}
					.recordings-container {
						margin-top: 740px;
					 }
					.test-run-data-region {
						width: calc(100% - 50px);
					}
				}
				@keyframes toggleVisible {
				  0%   {opacity: 0; z-index: -99;}
				  1%   {opacity: 0; z-index: 99}
				  100% {opacity: 1; z-index: 99}
				}
				@keyframes toggleHalfVisible {
				  0%   {opacity: 0; z-index: -98}
				  1%   {opacity: 0; z-index: 98;}
				  100% {opacity: 0.5; z-index: 98}
				}
			</style>
		</head>

		<div class='modal-background' onclick='$("".modal-close"").click();'></div>
		<div class='modal-popup'>
			<div class='modal-close' onclick='$(this).parent().css(""animation-direction"", ""reverse"");$("".modal-background"").css(""animation-direction"", ""reverse"");'>X</div>
			<h2 class='modal-title'>Stack Trace</h2>
			<div class='stacktrace-value'></div>
		</div>

		<svg class='header-logo' width='100' height='80' viewBox='0 0 89 32' xmlns='http://www.w3.org/2000/svg'>
			<g fill='currentColor' fill-rule='evenodd'>
				<path d='M28.487 0L15.42 3.405l-1.933 3.318-3.924-.029L0 15.995l9.564 9.3 3.922-.03 1.938 3.317 13.063 3.405 3.5-12.702-1.989-3.29 1.989-3.29L28.487 0zM13.802 7.257l9.995-2.498-5.737 9.665H6.584l7.218-7.167zm0 17.474l-7.218-7.166H18.06l5.737 9.664-9.995-2.498zm12.792.927l-5.74-9.663 5.74-9.667 2.771 9.667-2.771 9.663zM58.123 9.424c-1.746 0-2.918.723-3.791 2.095h-.075V9.773h-3.055v12.795h3.13V15.31c0-1.746 1.097-2.943 2.594-2.943 1.421 0 2.48.843 2.48 2.345v7.856h3.131v-8.355c0-2.794-1.77-4.789-4.414-4.789M46.44 17.15c0 1.696-.973 2.893-2.57 2.893-1.446 0-2.356-.823-2.356-2.32V9.767h-3.13v8.53c0 2.793 1.596 4.614 4.44 4.614 1.795 0 2.793-.673 3.666-1.845h.074v1.496h3.01V9.767H46.44v7.383M64.178 22.568h3.131V9.773h-3.13zM64.178 8.354h3.131V5.783h-3.13zM85.002 9.773l-1.86 5.761c-.4 1.173-.748 2.794-.748 2.794h-.08s-.424-1.621-.823-2.794l-2.1-5.76h-3.347l3.442 9.102c.723 1.946.972 2.769.972 3.467 0 1.048-.548 1.746-1.895 1.746h-1.197v2.669h1.995c2.594 0 3.494-1.023 4.467-3.866l4.511-13.119h-3.337M73.142 18.802v-6.784h1.995V9.773h-1.995v-3.99h-3.11v3.99h-1.77v2.245h1.77v7.507c0 2.42 1.822 3.068 3.468 3.068 1.346 0 1.712-.05 1.712-.05v-2.474s-.374.005-.798.005c-.749 0-1.272-.325-1.272-1.272'></path>
			</g>
		</svg>

		<div class='header-region'>
			<h1  class='header-title'>Local Test Run Report</h1>
		</div>

		<div class='status-summary-region'>
			<div class='test-run-data-region'>
				<div class='test-run-data'><div class='data-label'>Time Started UTC:</div><div id='start_time' class='data-value'></div></div>
				<div class='test-run-data'><div class='data-label'>Total Run Time:</div><div id='run_time' class='data-value'></div></div>
				<div class='test-run-data'><div class='data-label'>Device Type:</div><div id='device_type' class='data-value'></div></div>
				<div class='test-run-data'><div class='data-label'>Device Model:</div><div id='device_model' class='data-value'></div></div>
				<div class='test-run-data'><div class='data-label'>Device UDID:</div><div id='device_udid' class='data-value'></div></div>
				<div class='test-run-data'><div class='data-label'>Aspect Ratio:</div><div id='aspect_ratio' class='data-value'></div></div>
				<div class='test-run-data'><div class='data-label'>Resolution:</div><div id='resolution' class='data-value'></div></div>
			</div>
			<div id='piechart'></div>
			<div class='piechart-message-tooltip' onclick='$(this).hide();$("".piechart-messages"").css(""display"",""inline-block"");'>Click pie slice to view associated messages and filter visible tests by status. Click again to view all tests and all messages, regardless of status. Click here to view all messages. Click on exceptions to show a full stack trace.</div>
			<div class='piechart-messages'></div>
		</div>
		<div class='recordings-container'></div>

		<input class='pie-chart-data' type='hidden' value =''/>
	";

}