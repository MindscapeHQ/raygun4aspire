const path = require('path');
const CopyPlugin = require('copy-webpack-plugin');

module.exports = {
  entry: './wwwroot/js/site.js', // Path to your main JS file
  output: {
    path: path.resolve(__dirname, 'wwwroot/js'), // Output directory
    filename: 'bundle.js', // Output bundle file name
  },
  plugins: [
    new CopyPlugin({
      patterns: [
        { from: 'node_modules/moment/min/moment.min.js', to: 'moment.min.js' },
      ],
    }),
  ],
};